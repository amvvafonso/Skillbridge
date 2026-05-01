using Skillbridge.Models;

namespace Skillbridge.ViewModels;

public class ProfileViewModel
{
    public Organization Organization { get; set; }
    public bool Loading { get; set; }
    public string Message { get; set; }

}