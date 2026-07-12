using Microsoft.AspNetCore.Mvc;
using Skillbridge.Services;

namespace Skillbridge.Controllers;

[ApiController]
[Route("api/images")]
public class ImagesController(IS3Api s3Api) : ControllerBase
{
    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return NotFound();

        var result = await s3Api.GetBinaryAsync("logos", key);
        return result == null ? NotFound() : File(result.Value.Data, result.Value.ContentType);
    }
}