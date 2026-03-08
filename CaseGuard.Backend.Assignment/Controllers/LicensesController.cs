using CaseGuard.Backend.Assignment.Contracts.Licenses;
using CaseGuard.Backend.Assignment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaseGuard.Backend.Assignment.Controllers;

[ApiController]
[Route("api/licenses")]
[Authorize(Roles = "Admin")]
public class LicensesController(LicenseService svc) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLicenseRequest req)
    {
        var result = await svc.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
        => Ok(await svc.ListAsync(page, Math.Min(pageSize, 100), status, organizationId, sortBy, sortOrder));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => Ok(await svc.GetAsync(id));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLicenseRequest req)
        => Ok(await svc.UpdateAsync(id, req));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await svc.DeleteAsync(id);
        return NoContent();
    }
}
