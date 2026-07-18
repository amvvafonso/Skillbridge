using Skillbridge.Models.Client;

namespace Skillbridge.Models;

/// <summary>
/// Representa um token de API gerado por um utilizador, usado para autenticação
/// programática (fora do fluxo normal de login).
/// </summary>
public class ApiToken
{
    /// <summary>
    /// Id do token
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Id do utilizador a que o token pertence
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Utilizador a que o token pertence
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Nome/descrição atribuída ao token pelo utilizador
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hash do token
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Data de criação do token
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Data da última utilização do token
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Indica se o token ainda é valido
    /// </summary>
    public bool IsRevoked { get; set; }
}