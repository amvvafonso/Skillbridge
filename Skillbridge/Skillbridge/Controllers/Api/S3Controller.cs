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
    
    [HttpGet("buckets")]
    public async Task<IActionResult> GetBuckets()
    {
        var buckets = await is3Api.ListBucketsAsync();

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
        var file = await is3Api.GetBinaryAsync(bucket, key);

        if (file == null)
            return NotFound();

        return File(file.Value.Data, file.Value.ContentType);
    }
}