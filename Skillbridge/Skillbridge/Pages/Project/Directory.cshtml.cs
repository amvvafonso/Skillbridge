using System.Net;
using System.Security.Claims;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Skillbridge.Models.Utils;
using Microsoft.EntityFrameworkCore;
using File = Skillbridge.Models.Project.File;

namespace Skillbridge.Pages.Project;


[Authorize]
public class Directory : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly S3Api s3Api;
    public Directory(ApplicationDbContext context, S3Api s3Api)
    {
        _context = context;
        s3Api = s3Api;
    }

    public List<Amazon.S3.Model.S3Bucket> Buckets { get; set; } = new();
    public List<Amazon.S3.Model.S3Object> Files { get; set; } = new();
    public string CurrentBucket { get; set; } = string.Empty;
    public string CurrentPrefix { get; set; } = string.Empty;
    public List<string> Folders { get; set; } = new();
    
    public Models.Project.Project? CurrentProject { get; set; } = new();
    public string ViewMode { get; set; } = "grid";
    public Role UserPerm { get; set; } = Role.Unknown;
    
    public IActionResult OnGet(string? bucket, string? prefix, string? viewMode)
    {
        try
        {

            var user = User.FindFirstValue(ClaimTypes.NameIdentifier);

            UserPerm = _context.OrganizationMembers
                .Join(_context.Project,
                    orgM => orgM.Organization,
                    projectM => projectM.OrganizationId,
                    ((orgM, project) => new { orgM, project })
                )
                .Where(p => p.orgM.User == user && p.project.ProjectDirectory == bucket)
                .Select(p => p.orgM.Role)
                .FirstOrDefault();
            
            
            // Gets which organization the user belongs to
            var orgsMember = _context.Users
                .Join(_context.OrganizationMembers,
                    user => user.Id,
                    om => om.User, // ajusta ao nome real da FK para o User
                    (user, om) => new { user, om }
                )
                .Join(_context.Organizations,
                    uom => uom.om.Organization, // FK no OrganizationMember que aponta para Organization
                    organization => organization.OrganizationId,
                    (uom, organization) => new {organization.OrganizationId, uom.user} // já seleciona diretamente a Organization
                )
                .Where(p => p.user.Id == user)
                .Select(p => p.OrganizationId)
                .Distinct()
                .ToList();

            // IF user doesn't belong to any organization there is not project since projects belong to organizations
            if (orgsMember.Count == 0)
            {
                return Page();
            }
            // Gets each organization's project
            List<string?> orgProjects = _context.Organizations
                .Join(_context.Project,
                    organization => organization.OrganizationId,
                    project => project.OrganizationId,
                    ((organization, project) => new { project.ProjectDirectory, organization.OrganizationId })
                )
                .Where(p => orgsMember.Contains(p.OrganizationId))
                .Select(p => p.ProjectDirectory)
                .ToList();

            // Verifies that there is at least 1 project
            if (orgProjects.Count == 0)
            {
                return Page();
            }

            
            

            // If all the conditions above meet then it gets the project and files
            // Gets all buckets from the S3
            var bucketList = s3Api.ListBucketsAsync().Result;
            // Sends it to the Page if not null
            bucketList.ForEach(b =>
            {
                if (orgProjects.Contains(b.BucketName))
                {
                    Buckets.Add(b);
                }
            });
            
            // Defines currentBucket for aesthetic elements
            CurrentBucket = bucket ??  string.Empty;
            CurrentPrefix = prefix ?? string.Empty;
            // Defines view mode
            ViewMode = viewMode ?? "grid";

            // Gets all files from the current bucket selected

            // Verifies that the user has permission to access the bucket
            if (!orgProjects.Contains(CurrentBucket))
            {
                return LocalRedirect("/");

            }

            // Fetches all files from bucket
            var filelist = s3Api.ListFilesAsync(CurrentBucket).Result;
            Files = filelist ?? new List<Amazon.S3.Model.S3Object>();

            // Extract unique folder names from files that share the current prefix
            Folders = Files
                .Where(f => (f.Key.StartsWith(CurrentPrefix) && f.Key != CurrentPrefix))
                .Select(f =>
                {
                    var remainder = f.Key[CurrentPrefix.Length..];
                    var slashIdx = remainder.IndexOf('/');
                    return slashIdx > 0 ? remainder[..(slashIdx + 1)] : string.Empty;
                })
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            CurrentProject = _context.Project.FirstOrDefault(p => p.ProjectDirectory == bucket);
            
            // reloads page
            return Page();
        }
        catch (Exception ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToPage("Directory", new { bucket });
        }
    }

    public async Task<IActionResult> OnPostCreateFolderAsync([FromForm] string bucket, [FromForm] string prefix,[FromForm] string folderName) {
        try
        {
            
            // Initiates class
            // Verifies that the key is not empty and prepares it 
            var key = string.IsNullOrEmpty(prefix)
                ? $"{folderName.Trim('/')}/"
                : $"{prefix}{folderName.Trim('/')}/";

            // Create the folder on the S3 api
            var success = await s3Api.EditarFicheiroAsync(bucket, key, string.Empty);
            TempData["Message"] = success ? "Pasta criada com sucesso!" : "Erro ao criar pasta.";
            
            // Reloads the page
            return RedirectToPage("Directory", new { bucket, prefix });
        }
        catch (Exception ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToPage("Directory", new { bucket });
        }
    }

    public async Task<IActionResult> OnPostDeleteFileAsync([FromForm] string bucket, [FromForm] string key) {
        try
        {
            // Deletes the file from key
            var success = await s3Api.EliminarFicheiroAsync(bucket, key);
            TempData["Message"] = success ? "Ficheiro eliminado!" : "Erro ao eliminar ficheiro.";
            // Reloads the page
            return RedirectToPage("Directory", new { bucket, prefix = GetPrefixFromKey(key) });
        }
        catch (Exception ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToPage("Directory", new { bucket });
        }
    }

    public async Task<IActionResult> OnPostDeleteFolderAsync( [FromForm] string bucket, [FromForm] string folderPath) {
        try
        {
            var files = await s3Api.ListFilesAsync(bucket);
            // Verifies that the folder is empty, if it's not empty it deletes every file inside
            if (!files.IsNullOrEmpty())
            {
                foreach (var file in files.Where(f => f.Key.StartsWith(folderPath)).ToList())
                {
                    await s3Api.EliminarFicheiroAsync(bucket, file.Key);
                }
            }
            
            // Then deletes the bucket
            var success = await s3Api.EliminarFicheiroAsync(bucket, folderPath);
            // Status message
            TempData["Message"] = success ? "Pasta eliminada!" : "Erro ao eliminar pasta.";
            // relodas the page
            return RedirectToPage("Directory", new { bucket });
        }
        catch (Exception ex)
        {
            TempData["Message"] = ex.Message;
            return  RedirectToPage("Directory", new { bucket });
        }
    }

    
    public async Task<IActionResult> OnPostCreateSessionAsync([FromForm] string bucket, [FromForm] string prefix, [FromForm] string key, [FromForm] string title, [FromForm] string description, [FromForm] bool isPublic)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("Directory", new { bucket, prefix });

            // Ensure the File exists for the selected S3 key
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Path == key);
            if (file == null)
            {
                var project = await _context.Project.FirstOrDefaultAsync(p => p.ProjectDirectory == bucket);
                if (project == null)
                    return RedirectToPage("Directory", new { bucket, prefix });

                file = new File
                {
                    FileId = Guid.NewGuid().ToString(),
                    Path = key,
                    Locked = false,
                    ProjectId = project.ProjectId
                };
                await _context.Files.AddAsync(file);
                await _context.SaveChangesAsync();
            }

            // Verifies that the file has a session already active
            if (_context.Sessions.Any(p => p.Active && p.fileId==file.FileId))
            {
                TempData["message"] = "Já existe um sessão ativa com esse ficheiro!";
                return  RedirectToPage("Directory", new { bucket, prefix });
            }
            
            // Create the session
            var session = new Session
            {
                Id = Guid.NewGuid().ToString(),
                Title = title.Trim(),
                Description = description.Trim(),
                isPublic = isPublic,
                fileId = file.FileId,
                Active = true,
                Locked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Sessions.AddAsync(session);
            await _context.SaveChangesAsync();

            // Grant Mentor access to the current user
            var access = new SessionAccess
            {
                SessionAccessId = Guid.NewGuid().ToString(),
                SessionId = session.Id,
                UserId = userId,
                Role = Models.Client.Role.Mentor
            };
            await _context.SessionAccesses.AddAsync(access);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Sessão criada com sucesso!";
            return RedirectToPage("/CodeEditor", new { area = "Client", sessionId = session.Id });
        }
        catch (Exception ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToPage("Directory", new { bucket, prefix });
        }
    }
    
    [BindProperty]
    public IFormFile uploadedFile { get; set; }
    public async Task<IActionResult> OnPostUploadAsync([FromForm] string bucket, [FromForm] string prefix, [FromForm] int project)
    {
        try
        {
            Console.WriteLine("Bucket -->" + bucket);
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["Message"] = "Ficheiro inválido!";
                return RedirectToPage("Directory", new { bucket });
            }
            
            using var stream = uploadedFile.OpenReadStream();

            var transferUtility = new TransferUtility(s3Api.GetS3Client());

            var key = string.IsNullOrEmpty(prefix)
                ? $"{uploadedFile.FileName.Trim('/')}"
                : $"{prefix}/{uploadedFile.FileName}";
            
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = key,
                BucketName = bucket,
                ContentType = uploadedFile.ContentType,
            };
            
            await transferUtility.UploadAsync(uploadRequest);

            var file = new File();
            file.Path = key;
            file.FileId = Guid.NewGuid().ToString();
            file.Locked = false;
            file.ProjectId = project;
            
            await _context.Files.AddAsync(file);
            
            await _context.SaveChangesAsync();
            
            TempData["Message"] = "Ficheiro enviado com sucesso!";
            
            return RedirectToPage("Directory", new { bucket, prefix });
            
        }
        catch (Exception ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToPage("Directory", new { bucket, prefix });
        }
        
    }
    
    
    private static string GetPrefixFromKey(string key)
    {
        var lastSlash = key.LastIndexOf('/');
        return lastSlash > 0 ? key[..lastSlash] : string.Empty;
    }

    // ── Helpers exposed to the Razor view ──

    public static string GetIconClass(string ext) => ext switch
    {
        "cs" or "cshtml" or "csproj" => "fe-type-icon-code",
        "js" or "ts" or "jsx" or "tsx" => "fe-type-icon-code",
        "html" or "htm" => "fe-type-icon-html",
        "css" or "scss" or "less" => "fe-type-icon-css",
        "json" or "xml" => "fe-type-icon-json",
        "md" or "txt" => "fe-type-icon-text",
        "png" or "jpg" or "jpeg" or "gif" or "svg" or "webp" => "fe-type-icon-image",
        "zip" or "rar" or "7z" or "tar" or "gz" => "fe-type-icon-archive",
        "pdf" => "fe-type-icon-pdf",
        _ => "fe-type-icon-default"
    };

    public static string GetTypeIcon(string ext) => ext switch
    {
        "cs" or "cshtml" or "csproj" or "js" or "ts" or "jsx" or "tsx" or "html" or "htm" or "css" or "scss" => "bi-file-earmark-code",
        "json" or "xml" => "bi-braces",
        "md" or "txt" => "bi-file-earmark-text",
        "png" or "jpg" or "jpeg" or "gif" or "svg" or "webp" => "bi-file-earmark-image",
        "zip" or "rar" or "7z" or "tar" or "gz" => "bi-file-earmark-zip",
        "pdf" => "bi-file-earmark-pdf",
        _ => "bi-file-earmark"
    };

    public static string GetTypeColorClass(string ext) => ext switch
    {
        "cs" or "cshtml" => "fe-color-blue",
        "js" or "ts" => "fe-color-yellow",
        "html" => "fe-color-orange",
        "css" or "scss" => "fe-color-purple",
        "json" => "fe-color-green",
        "md" or "txt" => "fe-color-gray",
        "png" or "jpg" or "jpeg" or "gif" or "svg" => "fe-color-pink",
        "zip" or "rar" => "fe-color-red",
        "pdf" => "fe-color-red",
        _ => "fe-color-gray"
    };

    public static string FormatSize(long? bytes)
    {
        var b = bytes ?? 0;
        return b switch
        {
            < 1024 => $"{b} B",
            < 1024 * 1024 => $"{b / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{b / (1024.0 * 1024):F1} MB",
            _ => $"{b / (1024.0 * 1024 * 1024):F2} GB"
        };
    }

    public static string GetBreadcrumbPath(string fullPrefix, string segment)
    {
        var parts = fullPrefix.TrimEnd('/').Split('/');
        var idx = Array.IndexOf(parts, segment);
        if (idx < 0) return segment;
        return string.Join("/", parts[..(idx + 1)]);
    }
}
