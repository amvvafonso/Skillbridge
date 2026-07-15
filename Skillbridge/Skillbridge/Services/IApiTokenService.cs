using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Utilities;

namespace Skillbridge.Services;

public interface IApiTokenService
{

    Task<Result> CreateTokenAsync(string userId, string name);
    Task<Result?> ValidateTokenAsync(string rawToken); // devolve o userId, ou null se inválido
    Task<List<ApiToken>> GetUserTokensAsync(string userId);
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