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
public class S3Controller(IS3Api is3Api) : ControllerBase
{
    
    /// <summary>
    /// Vai buscar todos os buckets que o utilizador pode aceder
    /// </summary>
    /// <returns>Retorna uma lista de buckets</returns>
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

    /// <summary>
    /// Vai buscar os ficheiros de um determinado bucket
    /// </summary>
    /// <param name="bucket">Nome do bucket</param>
    /// <returns>List de ficheiros</returns>
    [HttpGet("files")]
    public async Task<IActionResult> GetFiles(string bucket)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        var files = await is3Api.ListFilesAsync(bucket, userId);

        if (files == null)
            return BadRequest("Erro ao obter ficheiros.");

        return Ok(files);
    }

    /// <summary>
    /// Faz download do ficheiro do bucket
    /// </summary>
    /// <param name="bucket">Nome do bucket</param>
    /// <param name="key">Key do ficheiro</param>
    /// <returns>Retorna o ficheiro</returns>
    [HttpGet("download")]
    public async Task<IActionResult> Download(string bucket, string key)
    {
        var file = await is3Api.GetBinaryAsync(bucket, key);

        if (file == null)
            return NotFound();

        return File(file.Value.Data, file.Value.ContentType);
    }
}