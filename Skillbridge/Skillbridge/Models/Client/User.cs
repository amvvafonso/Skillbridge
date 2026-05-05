using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Skillbridge.Models.Project;

namespace Skillbridge.Models.Client;

public class User : IdentityUser
{
    /// <summary>
    /// First name of user
    /// </summary>
    [Required(ErrorMessage =  "{0} é de preenchimento obrigatorio")]
    public string Name { get; set; }
    
    // ############################################################
    // Relacionamentos M-N
    // ############################################################
    public ICollection<UserProjectAccess>  UserProjectAccessList { get; set; } = []; 
}