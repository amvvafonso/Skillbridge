using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;

namespace Skillbridge.Areas.Client.Models;

public class IndexViewModel
{
    public User User { get; set; }
    public List<Session> Sessions { get; set; }
    public List<Organization?> Organizations { get; set; }
    public List<Project> Projects { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalProjects { get; set; }
    public int TotalOrganizations { get; set; }
}
