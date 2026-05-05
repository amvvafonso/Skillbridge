using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Models;
// Supondo que a sua classe User esteja neste namespace
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using File = Skillbridge.Models.Project.File;

namespace Skillbridge.Data;

// 1. Altere de IdentityDbContext para IdentityDbContext<User>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
    : IdentityDbContext<User>(options) 
{
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Project> Project { get; set; }
    public DbSet<File> Files { get; set; }
    public DbSet<UserProjectAccess> UserProjectAccesses { get; set; }

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