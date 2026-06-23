using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Models.Client;

namespace Skillbridge.Models;

public class Post
{
    /// <summary>
    /// Identifier of post PK
    /// </summary>
    [Key]
    public string PostId { get; set; }
    
    /// <summary>
    /// Title of post
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// Content of post, max(500)
    /// </summary>
    public string Content { get; set; }
    
    /// <summary>
    /// Determines if post is visible to the public
    /// </summary>
    public bool Visible { get; set; }
    
    /// <summary>
    /// When the posts was created
    /// </summary>
    public DateTime Created { get; set; }
    
    /// <summary>
    /// Foreign key, links user to post
    /// </summary>
    public string AuthorID { get; set; }
    
    [ForeignKey(nameof(AuthorID))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual User Author { get; set; }
    
    /// <summary>
    /// Foreign key, links post to organization
    /// </summary>
    public string OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual Organization Organization { get; set; }
}