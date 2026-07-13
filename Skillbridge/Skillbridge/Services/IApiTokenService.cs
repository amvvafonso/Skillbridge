using System.Security.Cryptography;
using System.Text;
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
    
    public class ApiTokenService(ApplicationDbContext context) : IApiTokenService
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

            return Result.Ok("Sucesso", rawToken); // única vez que o valor em claro existe
        }

        public async Task<Result?> ValidateTokenAsync(string rawToken)
        {
            var hash = HashToken(rawToken);

            var token = await context.ApiTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash && !t.IsRevoked);

            if (token == null) return Result.Fail("Token não encontrado!", ErrorType.Denied);

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
            }
        }

        private static string HashToken(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}