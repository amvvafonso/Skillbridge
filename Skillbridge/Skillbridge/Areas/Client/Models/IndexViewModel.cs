using Skillbridge.Models.Client;
using Skillbridge.Models.Project;

namespace Skillbridge.Areas.Client.Models;

public class IndexViewModel
{
    public User User { get; set; }
    public string test { get; set; }
    public List<Session> Session { get; set; } 
    public int SessionCount { get; set; }
}