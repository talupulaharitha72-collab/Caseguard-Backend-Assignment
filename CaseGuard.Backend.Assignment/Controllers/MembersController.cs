using CaseGuard.Backend.Assignment.Contracts.Members;
using CaseGuard.Backend.Assignment.Extensions;
using CaseGuard.Backend.Assignment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaseGuard.Backend.Assignment.Controllers;

[ApiController]
[Route("api/organizations/{orgId:guid}/members")]
[Authorize]
public class MembersController(OrganizationService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        Guid orgId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
        => Ok(await svc.ListMembersAsync(orgId, HttpContext.GetCurrentUserId(), page, Math.Min(pageSize, 100), search, sortBy, sortOrder));

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid orgId, Guid userId)
        => Ok(await svc.GetMemberAsync(orgId, userId, HttpContext.GetCurrentUserId()));

    [HttpPut("{userId:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid orgId, Guid userId, [FromBody] UpdateMemberRoleRequest req)
        => Ok(await svc.UpdateMemberRoleAsync(orgId, userId, req.Role, HttpContext.GetCurrentUserId()));

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Remove(Guid orgId, Guid userId)
    {
        await svc.RemoveMemberAsync(orgId, userId, HttpContext.GetCurrentUserId());
        return NoContent();
    }

    [HttpPost("{userId:guid}/license")]
    public async Task<IActionResult> AssignLicense(Guid orgId, Guid userId)
    {
        await svc.AssignLicenseAsync(orgId, userId, HttpContext.GetCurrentUserId());
        return Ok(new { message = "License assigned successfully" });
    }

    [HttpDelete("{userId:guid}/license")]
    public async Task<IActionResult> UnassignLicense(Guid orgId, Guid userId)
    {
        await svc.UnassignLicenseAsync(orgId, userId, HttpContext.GetCurrentUserId());
        return NoContent();
    }
}
