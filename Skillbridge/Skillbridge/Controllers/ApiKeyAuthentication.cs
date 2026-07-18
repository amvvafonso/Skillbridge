// Auth/ApiKeyAuthenticationHandler.cs

using Skillbridge.Services;

namespace Skillbridge.Auth;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Skillbridge.Services;

/// <inheritdoc />
public class ApiKeyAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiTokenService tokenService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>
    /// Autentica o token forneceido
    /// </summary>
    /// <returns>Coloca o userid como o user a qual o token corresponde</returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var raw = authHeader.ToString();
        if (!raw.StartsWith("Bearer "))
            return AuthenticateResult.NoResult();

        var token = raw["Bearer ".Length..].Trim();
        var userId = await tokenService.ValidateTokenAsync(token);

        if (userId?.Additional == null)
            return AuthenticateResult.Fail("Token inválido");

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.Additional) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}