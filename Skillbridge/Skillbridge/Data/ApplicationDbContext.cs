using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Models;
using Project = Skillbridge.Models.Project.Project;

namespace Skillbridge.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> User {get; set;}
    public DbSet<Project> Project { get; set; }

    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(string) && (property.IsKey() || property.IsIndex()))
                {
                    property.SetMaxLength(450);
                }
            }
        }
    }
}