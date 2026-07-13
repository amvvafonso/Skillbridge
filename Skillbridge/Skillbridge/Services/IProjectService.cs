using System.Runtime.InteropServices.JavaScript;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Utilities;

namespace Skillbridge.Services;

public interface IProjectService
{
    Task<List<Project>?> GetAllProjectAsync(string userId);
    Task<Project?> GetProjectAsync(string userId, int projectId);
    Task<Result> CreateFolderAsync(string bucket, string prefix, string folderName, string userId, int projectId);
    Task<Result> DeleteFolderAsync(string bucket, string folderPath, string userId);
    Task<Result> DeleteFileAsync(string bucket, string key, string userId);
    Task<Result> CreateProjectAsync(string organizationId, string userId, string projectName, string projectDescription, string? repository, bool isPublic);
    
    public class ProjectService(ApplicationDbContext context, IS3Api iS3Api, IOrganizationService organizationService, IS3Api is3Api) :  IProjectService
    {
        
        public async Task<List<Project>?> GetAllProjectAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            
            var userProjects = await context.UserProjectAccesses
                .Join(context.Project,
                    pj => pj.ProjectId,
                    usp => usp.ProjectId,
                    (usp, pj) => new { pj, usp })
                .Join(context.Organizations,
                    prev => prev.pj.OrganizationId,
                    org => org.OrganizationId,
                    (prev, org) => new { prev.pj, prev.usp, org })
                .Where(upa => upa.usp.UserId == userId)
                .Select(upa => new Project(upa.pj, upa.org))
                .ToListAsync();
            
            if (userProjects.IsNullOrEmpty()) return new List<Project>();
            
            
            return userProjects;
        }

        public async Task<Project?> GetProjectAsync(string userId, int projectId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            
            var project = await context.UserProjectAccesses
                .Join(context.Project,
                    pj => pj.ProjectId,
                    usp => usp.ProjectId,
                    (usp, pj) => new { pj, usp })
                .Join(context.Organizations,
                    prev => prev.pj.OrganizationId,
                    org => org.OrganizationId,
                    (prev, org) => new { prev.pj, prev.usp, org })
                .Where(upa => upa.usp.UserId == userId && upa.pj.ProjectId == projectId)
                .Select(upa => new Project(upa.pj, upa.org))
                .FirstOrDefaultAsync();
            
            return project;
        }

        public async Task<Result> CreateProjectAsync(string organizationId, string userId, string projectName, string projectDescription, string? repository, bool isPublic)
        {

            if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(userId))
            {
                return Result.Fail("Nenhuma organização e/ou user colocado!", ErrorType.MissingComponent);   
            }
        
            if (string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(projectDescription))
            {
                return Result.Fail("É obrigatório preencher o nome do projeto e a descrição!", ErrorType.MissingComponent);
            }

            if (await organizationService.MemberBelongsToOrganization(organizationId, userId) == null)
            {
                return Result.Fail("Membro não tem permissão para a seguinte operação", ErrorType.Denied);
            }

            var newProject = new Project
            {
                OrganizationId = organizationId,
                ProjectDescription = projectDescription,
                ProjectName = projectName,
                Public = isPublic,
                Repository = repository,
                ProjectDirectory = projectName
            };

            context.Project.Add(newProject);
        
            await context.SaveChangesAsync();
        
            // Adds every organization member to project 
            context.UserProjectAccesses.AddRange(
                context.OrganizationMembers
                    .Where(m => m.Organization == organizationId)
                    .Select(m => new UserProjectAccess(Role.Apprentice, m.User, newProject.ProjectId))
            );
        
            await context.SaveChangesAsync();
        
            try
            {
                await iS3Api.CriarBucketAsync(projectName);
            }
            catch {
                // Bucket creation not critical
            }

            return Result.Ok(message: "Projeto criado com sucesso!");
        }

        public async Task<Result> CreateFolderAsync(string bucket, string prefix, string folderName, string userId, int projectId)
        {
            if (string.IsNullOrWhiteSpace(folderName) || string.IsNullOrWhiteSpace(folderName))
            {
                return Result.Fail("É obrigatório fornecer um bucket e o nome da pasta!", ErrorType.MissingComponent);
            }

            if (!await HasPermission(userId, bucket))
            {
                return Result.Fail("Não tem permissão",  ErrorType.Denied);
            }
            
            var key = string.IsNullOrEmpty(prefix)
                ? $"{folderName.Trim('/')}/"
                : $"{prefix}{folderName.Trim('/')}";

            var success = await is3Api.EditarFicheiroAsync(bucket, key, string.Empty);
            return !success ? Result.Fail("Error ao criar a pasta", ErrorType.Misc) : Result.Ok(message: "Pasta criada com sucesso!");
        }

        public async Task<Result> DeleteFolderAsync(string bucket, string folderPath, string userId)
        {
            if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(folderPath))
                return Result.Fail("É obrigatório fornecer um bucket e a pasta!", ErrorType.MissingComponent);

            if (!await HasPermission(userId, bucket))
                return Result.Fail("Não tem permissão!", ErrorType.Denied);

            const int maxAttempts = 3;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var files = await is3Api.ListFilesAsync(bucket);

                if (files is { Count: > 0 })
                {
                    foreach (var file in files.Where(f => f.Key.StartsWith(folderPath)))
                    {
                        await is3Api.EliminarFicheiroAsync(bucket, file.Key);
                    }
                }

                var folderDeleted = await is3Api.EliminarFicheiroAsync(bucket, folderPath);

                if (folderDeleted)
                    return Result.Ok(message: "Pasta eliminada com sucesso!");

                // Se falhou e ainda há tentativas, espera um pouco e tenta outra vez
                if (attempt < maxAttempts)
                    await Task.Delay(300 * attempt);
            }

            return Result.Fail("Não foi possível eliminar a pasta — pode haver ficheiros a ser adicionados em simultâneo.", ErrorType.Misc);
        }

        public async Task<Result> DeleteFileAsync(string bucket, string key, string userId)
        {
            if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(key))
            {
                return Result.Fail("É obrigatório fornecer um bucket e uma key", ErrorType.MissingComponent);
            }

            if (!await HasPermission(userId, bucket))
            {
                return Result.Fail("Não tem permissão!", ErrorType.Denied);
            }
            
            var success = await is3Api.EliminarFicheiroAsync(bucket, key);

            return !success ? Result.Fail("Ocorreu um erro na eliminação do ficheiro!", ErrorType.Misc) : Result.Ok(message: "Pasta eliminada com sucesso!");
        }
        
        private async Task<bool> HasPermission(string userId, string bucket)
        {
            var buckets = await context.Project
                .Join(context.UserProjectAccesses,
                    p => p.ProjectId,
                    upa => upa.ProjectId,
                    ((project, access) => new { Project = project, Access = access }))
                .Where(p => p.Access.UserId == userId)
                .Select(p => p.Project.ProjectDirectory)
                .ToListAsync();
            if (buckets.IsNullOrEmpty() || !buckets.Contains(bucket))
            {
                return false;
            }

            return true;
        }
        
    }
}