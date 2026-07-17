using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Utilities;

namespace Skillbridge.Services;

/// <summary>
/// Serviço responsável pela criação, validação e revogação de tokens de acesso à API
/// Os tokens são armazenados como hash SHA-256, nunca em plain-text
/// </summary>
public interface IApiTokenService
{
    /// <summary>
    /// Gera um novo token de API para o user. O valor em plain-text do token
    /// só é devolvido nesta chamada e nunca mais fica acessível
    /// </summary>
    /// <param name="userId">Identificador do user proprietário do token</param>
    /// <param name="name">Nome descritivo do token</param>
    /// <returns>Um <see cref="Result"/> contendo o token gerado em plain-text</returns>
    Task<Result> CreateTokenAsync(string userId, string name);
   
    /// <summary>
    /// Valida um token de API fornecido, verificando se existe e não foi revogado.
    /// Atualiza a data de última utilização em caso de sucesso
    /// </summary>
    /// <param name="rawToken">Token em plain-text a validar</param>
    /// <returns>Um <see cref="Result"/> com o identificador do utilizador associado, ou falha se inválido</returns>
    Task<Result?> ValidateTokenAsync(string rawToken); // devolve o userId, ou null se inválido
   
    /// <summary>
    /// Obtém a lista de tokens ativos (não revogados) pertencentes a um utilizador,
    /// ordenados por data de criação descendente.
    /// </summary>
    /// <param name="userId">Identificador do user</param>
    /// <returns>Lista de <see cref="ApiToken"/> ativos</returns>
    Task<List<ApiToken>> GetUserTokensAsync(string userId);
  
    /// <summary>
    /// Revoga um token de API pertencente ao utilizador, tornando-o inválido
    /// para futuras autenticações
    /// </summary>
    /// <param name="tokenId">Identificador do token a revogar</param>
    /// <param name="userId">Identificador do utilizador proprietário do token</param>
    Task RevokeTokenAsync(int tokenId, string userId);
    
    public class ApiTokenService(ApplicationDbContext context, ILogger<ApiTokenService> logger) : IApiTokenService
    {
        public async Task<Result> CreateTokenAsync(string userId, string name)
        {
            var rawToken = "sb_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "").Replace("/", "").Replace("=", "");

            context.ApiTokens.Add(new ApiToken
            {
                UserId = userId,
                Name = name,
                TokenHash = HashToken(rawToken),
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            logger.LogInformation("Token API '{Name}' criado para o utilizador {UserId}", name, userId);
            
            return Result.Ok("Sucesso", rawToken); // única vez que o valor em claro existe
        }

        public async Task<Result?> ValidateTokenAsync(string rawToken)
        {
            var hash = HashToken(rawToken);

            var token = await context.ApiTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash && !t.IsRevoked);

            if (token == null)
            {
                logger.LogWarning("Tentativa de validação com token inválido ou revogado");
                return Result.Fail("Token não encontrado!", ErrorType.Denied);
            }

            token.LastUsedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return Result.Ok("Token validado com sucesso!", token.UserId);
        }

        public async Task<List<ApiToken>> GetUserTokensAsync(string userId) =>
            await context.ApiTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

        public async Task RevokeTokenAsync(int tokenId, string userId)
        {
            var token = await context.ApiTokens
                .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId);

            if (token != null)
            {
                token.IsRevoked = true;
                await context.SaveChangesAsync();
                logger.LogInformation("Token {TokenId} revogado pelo utilizador {UserId}", tokenId, userId);
            }
            else
            {
                logger.LogWarning("Utilizador {UserId} tentou revogar o token {TokenId} que não lhe pertence ou não existe", userId, tokenId);
            }
        }

        private static string HashToken(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}