using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models;
using Skillbridge.Services;

namespace Skillbridge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationMemberController(ApplicationDbContext context, IOrganizationMemberService organizationMemberService, IOrganizationService organizationService) : ControllerBase
{
    // GET: api/organizationmember/organization/{organizationId}
    [HttpGet("organization/{organizationId}")]
    public async Task<ActionResult<IEnumerable<OrganizationMember>>> GetMembersByOrganization(string organizationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        return await organizationMemberService.GetMembersAsync(organizationId);
    }
    
    
    
    [HttpPost("organization/{organizationId}/promote/{memberId}")]
    public async Task<ActionResult<IEnumerable<OrganizationMember>>> PromoteMember(string organizationId, string memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await organizationService.PromoteMemberAsync(memberId, organizationId, userId);

        if (result.Success)  return Ok(result.Message);

        switch (result.ErrorType)
        {
            case ErrorType.NotFound: return NotFound(result.Message);
            case ErrorType.Denied:  return Forbid(result.Message);
            default: return BadRequest(result.Message);
        }
    }
    

    // DELETE: api/organizationmember/{id}
    [HttpDelete("organization/{organizationId}/member/{memberId}")]
    public async Task<IActionResult> RemoveMember(string organizationId, string memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();

        var result = await organizationService.DeleteMemberAsync(memberId, organizationId, userId);

        if (result.Success)  return Ok(result.Message);

        switch (result.ErrorType)
        {
            case ErrorType.NotFound: return NotFound(result.Message);
            case ErrorType.Denied:  return Forbid(result.Message);
            default: return BadRequest(result.Message);
        }
    }
}