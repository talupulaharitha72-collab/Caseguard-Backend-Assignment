using CaseGuard.Backend.Assignment.Contracts.Invitations;
using CaseGuard.Backend.Assignment.Extensions;
using CaseGuard.Backend.Assignment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaseGuard.Backend.Assignment.Controllers;

[ApiController]
[Authorize]
public class InvitationsController(InvitationService svc) : ControllerBase
{
    [HttpPost("api/organizations/{orgId:guid}/invitations")]
    public async Task<IActionResult> Create(Guid orgId, [FromBody] CreateInvitationRequest req)
    {
        var result = await svc.CreateAsync(orgId, req, HttpContext.GetCurrentUserId());
        return CreatedAtAction(nameof(Get), new { orgId, invId = result.Id }, result);
    }

    [HttpGet("api/organizations/{orgId:guid}/invitations")]
    public async Task<IActionResult> List(
        Guid orgId,
        [FromQuery] string? status,
        [FromQuery] string? search = null,
        [FromQuery] string? sortOrder = null)
        => Ok(await svc.ListAsync(orgId, HttpContext.GetCurrentUserId(), status, search, sortOrder));

    [HttpGet("api/organizations/{orgId:guid}/invitations/{invId:guid}")]
    public async Task<IActionResult> Get(Guid orgId, Guid invId)
        => Ok(await svc.GetAsync(orgId, invId, HttpContext.GetCurrentUserId()));

    [HttpDelete("api/organizations/{orgId:guid}/invitations/{invId:guid}")]
    public async Task<IActionResult> Cancel(Guid orgId, Guid invId)
    {
        await svc.CancelAsync(orgId, invId, HttpContext.GetCurrentUserId());
        return NoContent();
    }

    [HttpPost("api/invitations/{invId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid invId)
    {
        await svc.AcceptAsync(invId, HttpContext.GetCurrentUserId(), HttpContext.GetCurrentUserEmail());
        return Ok(new { message = "Invitation accepted" });
    }
}
