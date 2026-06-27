using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models.Utils;

namespace Skillbridge.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class S3Controller : ControllerBase
{
    private readonly S3Api _s3Api;
    
    public S3Controller(S3Api s3Api)
    {
        _s3Api = s3Api;
    }
    
    [HttpGet("buckets")]
    public async Task<IActionResult> GetBuckets()
    {
        var buckets = await _s3Api.ListBucketsAsync();

        if (buckets == null)
            return BadRequest("Erro ao obter buckets.");

        return Ok(buckets);
    }

    [HttpGet("files")]
    public async Task<IActionResult> GetFiles(string bucket)
    {
        var files = await _s3Api.ListFilesAsync(bucket);

        if (files == null)
            return BadRequest("Erro ao obter ficheiros.");

        return Ok(files);
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download(string bucket, string key)
    {
        var file = await _s3Api.GetBinaryAsync(bucket, key);

        if (file == null)
            return NotFound();

        return File(file.Value.Data, file.Value.ContentType);
    }
}