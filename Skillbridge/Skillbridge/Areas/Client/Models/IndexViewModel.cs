using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;

namespace Skillbridge.Areas.Client.Models;

public class IndexViewModel
{
    /// <summary>
    /// Utilizador autenticado
    /// </summary>
    public User User { get; set; } = null!;
    /// <summary>
    /// Lista de sessão que tem acesso
    /// </summary>
    public List<Session> Sessions { get; set; } = new();
    /// <summary>
    /// Número de sessões ativas
    /// </summary>
    public int ActiveSessions { get; set; }
    /// <summary>
    /// Número de projetos que tem acesso
    /// </summary>
    public int TotalProjects { get; set; }
    /// <summary>
    /// Número de organizações a que pertence
    /// </summary>
    public int TotalOrganizations { get; set; }
    /// <summary>
    /// Lista de organizações que pertence
    /// </summary>
    public List<Organization> Organizations { get; set; } = new();
    /// <summary>
    /// Lista de projetos que tem acesso
    /// </summary>
    public List<Project> Projects { get; set; } = new();
}
