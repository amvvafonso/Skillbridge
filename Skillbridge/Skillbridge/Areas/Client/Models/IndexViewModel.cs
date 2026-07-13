using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;

namespace Skillbridge.Areas.Client.Models;

public class IndexViewModel
{
    public User User { get; set; } = null!;
    public List<Session> Sessions { get; set; } = new();
    public int ActiveSessions { get; set; }
    public int TotalProjects { get; set; }
    public int TotalOrganizations { get; set; }
    public List<Organization> Organizations { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
}
