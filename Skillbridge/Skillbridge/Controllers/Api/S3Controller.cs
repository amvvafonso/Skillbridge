using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Services;

namespace Skillbridge.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class S3Controller(IS3Api is3Api, ApplicationDbContext _context, ILogger<S3Controller> logger) : ControllerBase
{
    
    [HttpGet("buckets")]
    public async Task<IActionResult> GetBuckets()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        var buckets = await is3Api.ListBucketsAsync(userId);

        if (buckets == null)
            return BadRequest("Erro ao obter buckets.");

        return Ok(buckets);
    }

    [HttpGet("files")]
    public async Task<IActionResult> GetFiles(string bucket)
    {
        var files = await is3Api.ListFilesAsync(bucket);

        if (files == null)
            return BadRequest("Erro ao obter ficheiros.");

        return Ok(files);
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download(string bucket, string key)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (bucket != "logos" && bucket != "banners")
        {
            var hasAccess = await _context.Files
                .Where(f => f.FileId == key)
                .Join(_context.UserProjectAccesses, f => f.ProjectId, upa => upa.ProjectId, (f, upa) => upa)
                .AnyAsync(upa => upa.UserId == userId);

            if (!hasAccess)
            {
                logger.LogWarning("Utilizador {UserId} tentou aceder ao ficheiro {Key} sem permissão", userId, key);
                return Forbid();
            }
        }

        var file = await is3Api.GetBinaryAsync(bucket, key);
        if (file == null) return NotFound();

        return File(file.Value.Data, file.Value.ContentType);
    }
}