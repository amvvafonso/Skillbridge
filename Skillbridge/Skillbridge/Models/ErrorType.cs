namespace Skillbridge.Models;

/// <summary>
/// Enum dos tipos de erros possíveis nos serviços
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Sem acesso
    /// </summary>
    Denied,
    /// <summary>
    /// Não foi encontrado
    /// </summary>
    NotFound,
    /// <summary>
    /// Faltam componentes cruciais para o funcionamento do método
    /// </summary>
    MissingComponent,
    /// <summary>
    /// Miscellaneous, erros gerais
    /// </summary>
    Misc
}