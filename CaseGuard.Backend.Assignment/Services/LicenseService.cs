using CaseGuard.Backend.Assignment.Contracts.Common;
using CaseGuard.Backend.Assignment.Contracts.Licenses;
using CaseGuard.Backend.Assignment.Data;
using CaseGuard.Backend.Assignment.Exceptions;
using CaseGuard.Backend.Assignment.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseGuard.Backend.Assignment.Services;

public class LicenseService(ApplicationDbContext db)
{
    public async Task<LicenseResponse> CreateAsync(CreateLicenseRequest req)
    {
        var orgExists = await db.Organizations.AnyAsync(o => o.Id == req.OrganizationId);
        if (!orgExists) throw new NotFoundException("Organization not found");

        var license = new License
        {
            Id = Guid.NewGuid(),
            OrganizationId = req.OrganizationId,
            Status = LicenseStatus.Active,
            AutoRenew = req.AutoRenew,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };

        db.Licenses.Add(license);
        await db.SaveChangesAsync();

        return Map(license);
    }

    public async Task<PagedResponse<LicenseResponse>> ListAsync(int page, int pageSize, string? status, Guid? organizationId, string? sortBy = null, string? sortOrder = null)
    {
        var query = db.Licenses.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LicenseStatus>(status, true, out var parsedStatus))
            query = query.Where(l => l.Status == parsedStatus);

        if (organizationId.HasValue)
            query = query.Where(l => l.OrganizationId == organizationId.Value);

        var total = await query.CountAsync();

        var isDesc = !string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "expiresat" => isDesc ? query.OrderByDescending(l => l.ExpiresAt) : query.OrderBy(l => l.ExpiresAt),
            "status" => isDesc ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
            _ => isDesc ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => Map(l))
            .ToListAsync();

        return new PagedResponse<LicenseResponse>(items, total, page, pageSize);
    }

    public async Task<LicenseResponse> GetAsync(Guid id)
    {
        var license = await db.Licenses.FindAsync(id)
                      ?? throw new NotFoundException("License not found");
        return Map(license);
    }

    public async Task<LicenseResponse> UpdateAsync(Guid id, UpdateLicenseRequest req)
    {
        var license = await db.Licenses.FindAsync(id)
                      ?? throw new NotFoundException("License not found");

        if (req.ExpiresAt.HasValue) license.ExpiresAt = req.ExpiresAt.Value;
        if (req.AutoRenew.HasValue) license.AutoRenew = req.AutoRenew.Value;
        if (!string.IsNullOrWhiteSpace(req.Status))
        {
            if (!Enum.TryParse<LicenseStatus>(req.Status, true, out var status))
                throw new BadRequestException($"Invalid status: {req.Status}");
            license.Status = status;
        }

        await db.SaveChangesAsync();
        return Map(license);
    }

    public async Task DeleteAsync(Guid id)
    {
        var license = await db.Licenses.FindAsync(id)
                      ?? throw new NotFoundException("License not found");

        if (license.Status == LicenseStatus.Revoked)
            throw new BadRequestException("License is already revoked");

        license.Status = LicenseStatus.Revoked;
        await db.SaveChangesAsync();
    }

    private static LicenseResponse Map(License l)
        => new(l.Id, l.OrganizationId, l.AssignedUserId, l.Status.ToString(), l.AutoRenew, l.ExpiresAt, l.CreatedAt, l.RenewedAt);
}
