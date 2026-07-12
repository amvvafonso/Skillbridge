using System.Runtime.InteropServices.JavaScript;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Utilities;

namespace Skillbridge.Services;

public interface IProjectService
{
    Task<Result> CreateFolderAsync(string bucket, string prefix, string folderName, string userId, int projectId);
    Task<Result> DeleteFolderAsync(string bucket, string folderPath, string userId);
    Task<Result> DeleteFileAsync(string bucket, string key, string userId);
    
    
    public class ProjectService(ApplicationDbContext context, IS3Api is3Api, IOrganizationService organizationService) :  IProjectService
    {
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