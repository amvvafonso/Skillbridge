using System.Runtime.InteropServices.JavaScript;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Utilities;
using File = Skillbridge.Models.Project.File;

namespace Skillbridge.Services;

/// <summary>
/// Serviço responsável pela gestão de projetos dentro de uma organização,
/// incluindo criação, eliminação e gestão de pastas/ficheiros associados no S3
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Obtém todos os projetos a que o utilizador tem acesso, incluindo dados da organização respetiva
    /// </summary>
    /// <param name="userId">Identificador do utilizador</param>
    /// <returns>Lista de <see cref="Project"/>, ou <c>null</c> se o userId for inválido</returns>
    Task<List<Project>?> GetAllProjectAsync(string userId);
    
    /// <summary>
    /// Obtém um projeto específico, desde que o utilizador tenha acesso a ele
    /// </summary>
    /// <param name="userId">Identificador do utilizador</param>
    /// <param name="projectId">Identificador do projeto</param>
    /// <returns>O <see cref="Project"/> correspondente, ou <c>null</c> se não existir ou não houver acesso</returns>
    Task<Project?> GetProjectAsync(string userId, int projectId);
    
    /// <summary>
    /// Cria uma nova pasta num bucket S3, associada a um projeto
    /// Requer que o utilizador tenha acesso ao bucket do projeto
    /// </summary>
    /// <param name="bucket">Nome do bucket S3 onde a pasta será criada</param>
    /// <param name="prefix">Prefixo do caminho, se a pasta estiver dentro de outra pasta</param>
    /// <param name="folderName">Nome da nova pasta</param>
    /// <param name="userId">Identificador do utilizador que solicita a criação</param>
    /// <param name="projectId">Identificador do projeto associado</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> CreateFolderAsync(string bucket, string prefix, string folderName, string userId, int projectId);
    
    /// <summary>
    /// Elimina uma pasta e todos os ficheiros contidos nela num bucket S3, com nova tentativa
    /// automática em caso de falha (até 3 tentativas, com espera progressiva entre elas)
    /// </summary>
    /// <param name="bucket">Nome do bucket S3</param>
    /// <param name="folderPath">Caminho da pasta a eliminar</param>
    /// <param name="userId">Identificador do user que solicita a eliminação</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> DeleteFolderAsync(string bucket, string folderPath, string userId);
    
    /// <summary>
    /// Elimina um ficheiro do bucket S3 e remove o respetivo registo da base de dados, se existir
    /// </summary>
    /// <param name="bucket">Nome do bucket S3</param>
    /// <param name="key">Chave (path) do ficheiro a eliminar</param>
    /// <param name="userId">Identificador do utilizador que solicita a eliminação</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> DeleteFileAsync(string bucket, string key, string userId);
    
    /// <summary>
    /// Cria um novo projeto numa organização e concede acesso a todos os membros existentes
    /// da organização, com o mesmo papel que já tinham. Tenta criar um bucket S3 dedicado,
    /// mas a falha desta criação não impede o sucesso da operação
    /// </summary>
    /// <param name="organizationId">Identificador da organização proprietária do projeto</param>
    /// <param name="userId">Identificador do utilizador que cria o projeto</param>
    /// <param name="projectName">Nome do projeto, também usado como diretório do bucket S3</param>
    /// <param name="projectDescription">Descrição do projeto</param>
    /// <param name="repository">URL do repositório associado, se aplicável</param>
    /// <param name="isPublic">Indica se o projeto é visível publicamente</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> CreateProjectAsync(string organizationId, string userId, string projectName, string projectDescription, string? repository, bool isPublic);
    
    /// <summary>
    /// Elimina um projeto, incluindo todos os ficheiros associados no S3 e os acessos de utilizadores
    /// Requer que o utilizador tenha um papel superior a Apprentice na organização
    /// </summary>
    /// <param name="userId">Identificador do utilizador que solicita a eliminação</param>
    /// <param name="projectId">Identificador do projeto a eliminar</param>
    /// <returns>Um <see cref="Result"/> indicando sucesso ou falha da operação</returns>
    Task<Result> DeleteProjectAsync(string userId, int projectId);

    /// <inheritdoc />
    public class ProjectService(ApplicationDbContext context, IS3Api iS3Api, IOrganizationService organizationService, IS3Api is3Api, ILogger<IProjectService> logger) :  IProjectService
    {
        
        /// <summary>
        /// Classe auxiliar
        /// </summary>
        /// <param name="FileId"></param>
        /// <param name="Path"></param>
        /// <param name="ProjectId"></param>
        /// <param name="ProjectDirectory"></param>
        public record FileWithProjectDto(
            string FileId,
            string Path,
            int ProjectId,
            string ProjectDirectory
        );

        /// <inheritdoc cref="GetAllFileFromProjectAsync" />
        public async Task<List<FileWithProjectDto>> GetAllFileFromProjectAsync(string userId, int projectId)
        {
            if (projectId == 0) return [];

            var hasPermission = await context.UserProjectAccesses
                .AnyAsync(x => x.UserId == userId && x.ProjectId == projectId);

            if (!hasPermission) return [];
            
            logger.LogInformation("Getting file from project {ProjectId}", projectId);
            
            return await context.Files
                .Where(f => f.ProjectId == projectId)
                .Join(context.Project,
                    f => f.ProjectId,
                    p => p.ProjectId,
                    (f, p) => new FileWithProjectDto(
                        f.FileId,
                        f.Path ?? "",
                        f.ProjectId,
                        p.ProjectDirectory ?? "" 
                    ))
                .ToListAsync();
        }
        
        /// <inheritdoc />
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
            
            logger.LogInformation("Fetching all projects from {userId}", userId);
            return userProjects;
        }
        
        /// <inheritdoc />
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

        /// <inheritdoc />
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
        
            // Adds every organization member to project, 
            context.UserProjectAccesses.AddRange(
                context.OrganizationMembers
                    .Where(m => m.Organization == organizationId)
                    .Select(m => new UserProjectAccess(m.Role, m.User, newProject.ProjectId))
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

        /// <inheritdoc />
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

            var success = await is3Api.EditarFicheiroAsync(bucket, key, string.Empty, userId);
            return !success ? Result.Fail("Error ao criar a pasta") : Result.Ok(message: "Pasta criada com sucesso!");
        }

        /// <inheritdoc />
        public async Task<Result> DeleteFolderAsync(string bucket, string folderPath, string userId)
        {
            if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(folderPath))
                return Result.Fail("É obrigatório fornecer um bucket e a pasta!", ErrorType.MissingComponent);

            if (!await HasPermission(userId, bucket))
                return Result.Fail("Não tem permissão!", ErrorType.Denied);

            const int maxAttempts = 3;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var files = await is3Api.ListFilesAsync(bucket, userId);

                if (files is { Count: > 0 })
                {
                    foreach (var file in files.Where(f => f.Key.StartsWith(folderPath)))
                    {
                        await is3Api.EliminarFicheiroAsync(bucket, file.Key, userId);
                    }
                }

                var folderDeleted = await is3Api.EliminarFicheiroAsync(bucket, folderPath, userId);

                if (folderDeleted)
                    return Result.Ok(message: "Pasta eliminada com sucesso!");

                // Se falhou e ainda há tentativas, espera um pouco e tenta outra vez
                if (attempt < maxAttempts)
                    await Task.Delay(300 * attempt);
            }

            return Result.Fail("Não foi possível eliminar a pasta — pode haver ficheiros a ser adicionados em simultâneo.");
        }

        /// <inheritdoc />
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
            
            var success = await is3Api.EliminarFicheiroAsync(bucket, key, userId);

            var file = await context.Files.FirstOrDefaultAsync(f => f.Path == key);
            
            if (success || file != null)
                if (file != null)
                    context.Files.Remove(file);

            await context.SaveChangesAsync();
            
            return !success ? Result.Fail("Ocorreu um erro na eliminação do ficheiro!") : Result.Ok(message: "Pasta eliminada com sucesso!");
        }

        /// <inheritdoc />
        public async Task<Result> DeleteProjectAsync(string userId, int projectId)
        {
            if (projectId == 0) return Result.Fail("Falta componentes cruciais para a operação!", ErrorType.MissingComponent);

            var organization = await context.Organizations
                .Join(context.Project,
                    o => o.OrganizationId,
                    p => p.OrganizationId,
                    (organization1, project) => new { Organization = organization1, Project = project })
                .Where(p => p.Project.ProjectId == projectId)
                .FirstOrDefaultAsync();

            if (organization == null) return Result.Fail("Projeto não pertence a nenhuma organização!");
            
            
            var userRole = await context.OrganizationMembers.FirstOrDefaultAsync(ur => ur.User == userId && ur.Organization == organization.Organization.OrganizationId);

            if (userRole == null || userRole.Role == Role.Apprentice || userRole.Role == Role.Unknown) return Result.Fail("Não tem permissão",  ErrorType.Denied);
            
            var files = await GetAllFileFromProjectAsync(userId, projectId);
            
            foreach (var file in files)
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        await DeleteFileAsync(file.ProjectDirectory, file.Path, userId);
                        logger.LogInformation($"File {file.Path} has been deleted by {userId}.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error deleting file {file.Path}");
                        // Logs error and tries again
                    }
                }
            }
            
            context.UserProjectAccesses.RemoveRange(context.UserProjectAccesses
                .Where(upa => upa.ProjectId == projectId)
                .ToList());
            
            context.Remove(context.Project.FirstOrDefaultAsync(upa => upa.ProjectId == projectId));
            
            await context.SaveChangesAsync();
            
            return Result.Ok("Projeto eliminado com sucesso!");
        }
        
        private async Task<bool> HasPermission(string userId, string bucket)
        {
            var buckets = await context.Project
                .Join(context.UserProjectAccesses,
                    p => p.ProjectId,
                    upa => upa.ProjectId,
                    (project, access) => new { Project = project, Access = access })
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