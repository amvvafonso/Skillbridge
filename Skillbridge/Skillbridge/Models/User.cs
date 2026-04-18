using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Skillbridge.Models;

public class User
{
    /// <summary>
    /// Identifier of the user
    /// </summary>
    [Key]
    public int UserId { get; set; }

    /// <summary>
    /// Useraname of User, used for login
    /// </summary>
    [Required(ErrorMessage = "User name is required")]
    [MaxLength(50, ErrorMessage = "User name cannot be longer than 50 characters")]
    public string? Username { get; set; }

    /// <summary>
    /// Email of the User
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>
    /// Password of the User
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [MaxLength(100, ErrorMessage = "Password cannot be longer than 100 characters")]
    public string? Password { get; set; }

    /// <summary>
    /// Role of the account
    /// </summary>
    [Required(ErrorMessage = "Role is required  ")]
    public Role Role { get; set; }

    // Useful information linked to user

    /// <summary>
    /// Timestamp of the user creation
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime TimeOfCreation { get; set; } = DateTime.UtcNow;
}
