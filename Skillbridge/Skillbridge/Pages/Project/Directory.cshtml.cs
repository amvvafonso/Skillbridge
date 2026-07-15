using System.Security.Claims;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Skillbridge.Data;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Skillbridge.Models;
using Skillbridge.Services;
using File = Skillbridge.Models.Project.File;

namespace Skillbridge.Pages.Project;


[Authorize]
public class Directory(ApplicationDbContext context, IS3Api is3Api, IProjectService projectService, ISessionService sessionService) : PageModel
{

    public List<S3Bucket> Buckets { get; set; } = new();
    public List<S3Object> Files { get; set; } = new();
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

            UserPerm = context.OrganizationMembers
                .Join(context.Project,
                    orgM => orgM.Organization,
                    projectM => projectM.OrganizationId,
                    ((orgM, project) => new { orgM, project })
                )
                .Where(p => p.orgM.User == user && p.project.ProjectDirectory == bucket)
                .Select(p => p.orgM.Role)
                .FirstOrDefault();
            
            
            // Gets which organization the user belongs to
            var orgsMember = context.Users
                .Join(context.OrganizationMembers,
                    user => user.Id,
                    om => om.User, // ajusta ao nome real da FK para o User
                    (user, om) => new { user, om }
                )
                .Join(context.Organizations,
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
            List<string?> orgProjects = context.Organizations
                .Join(context.Project,
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
            var bucketList = is3Api.ListBucketsAsync(user).Result;
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
            var filelist = is3Api.ListFilesAsync(CurrentBucket, user).Result;
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

            CurrentProject = context.Project.FirstOrDefault(p => p.ProjectDirectory == bucket);
            
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

        var user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await projectService.CreateFolderAsync(bucket, prefix, folderName, user, 1);
        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid();
            case ErrorType.NotFound: return NotFound();
        }
            
        TempData["ToastType"] = result.Success ? "success" :  "danger";
        TempData["Message"] = result.Message;
            
        // Reloads the page
        return RedirectToPage("Directory", new { bucket, prefix });
    }

    public async Task<IActionResult> OnPostDeleteFileAsync([FromForm] string bucket, [FromForm] string key) {

        var user =  User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user == null) return RedirectToPage("/Account/Login");
        
        var result = await projectService.DeleteFileAsync(bucket, key, user);

        switch (result.ErrorType)
        {
            case ErrorType.Denied: return Forbid();
            case ErrorType.NotFound: return NotFound(); 
        }
                
        TempData["ToastType"] =  result.Success ? "success" :  "danger";
        TempData["Message"] = result.Message;
        return RedirectToPage("Directory", new { bucket, prefix = GetPrefixFromKey(key) });

    }

    // Done
    public async Task<IActionResult> OnPostDeleteFolderAsync( [FromForm] string bucket, [FromForm] string folderPath) {
        var user =  User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user == null) return RedirectToPage("/Account/Login");
        
        var result = await projectService.DeleteFolderAsync(bucket, folderPath, user);

        switch (result.ErrorType)
        {
            case  ErrorType.Denied: return Forbid();
            case  ErrorType.NotFound: return NotFound();
        }
        TempData["ToastType"] = result.Success ? "success" :  "danger";
        TempData["Message"] = result.Message;
        return RedirectToPage("Directory", new { bucket });
    }

    //Done
    public async Task<IActionResult> OnPostCreateSessionAsync([FromForm] string bucket, [FromForm] string prefix, [FromForm] string key, [FromForm] string title, [FromForm] string description, [FromForm] bool isPublic)
    {
 
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("Directory", new { bucket, prefix });

        var result = await sessionService.CreateSessionAsync(bucket, key, title, description, isPublic, userId);

        if (result.Success)
            return RedirectToPage("/CodeEditor",
                new { area = "Client", sessionId = result.Additional }); // Aditional is the new session id
            
        TempData["Message"] = result.Message;
        TempData["ToastType"] = result.Success ? "success" :  "danger";
        return Page();
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

            var transferUtility = new TransferUtility(is3Api.GetS3Client());

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
            
            await context.Files.AddAsync(file);
            
            await context.SaveChangesAsync();
            
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
