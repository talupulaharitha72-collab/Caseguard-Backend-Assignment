using CaseGuard.Backend.Assignment.Contracts.Organizations;
using CaseGuard.Backend.Assignment.Extensions;
using CaseGuard.Backend.Assignment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaseGuard.Backend.Assignment.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize]
public class OrganizationsController(OrganizationService svc) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest req)
    {
        var result = await svc.CreateAsync(req, HttpContext.GetCurrentUserId());
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
        => Ok(await svc.ListForUserAsync(HttpContext.GetCurrentUserId(), search, sortBy, sortOrder));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => Ok(await svc.GetAsync(id, HttpContext.GetCurrentUserId()));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationRequest req)
        => Ok(await svc.UpdateAsync(id, req, HttpContext.GetCurrentUserId()));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await svc.DeleteAsync(id, HttpContext.GetCurrentUserId());
        return NoContent();
    }
}
