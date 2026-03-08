using CaseGuard.Backend.Assignment.Contracts.Common;
using CaseGuard.Backend.Assignment.Contracts.Members;
using CaseGuard.Backend.Assignment.Contracts.Organizations;
using CaseGuard.Backend.Assignment.Data;
using CaseGuard.Backend.Assignment.Exceptions;
using CaseGuard.Backend.Assignment.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseGuard.Backend.Assignment.Services;

public class OrganizationService(ApplicationDbContext db)
{
    public async Task<OrganizationResponse> CreateAsync(CreateOrganizationRequest req, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new BadRequestException("Organization name is required");

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Description = req.Description,
            CreatedAt = DateTime.UtcNow
        };

        var member = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = userId,
            Role = OrganizationRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        db.Organizations.Add(org);
        db.OrganizationMembers.Add(member);
        await db.SaveChangesAsync();

        return Map(org);
    }

    public async Task<List<OrganizationResponse>> ListForUserAsync(Guid userId, string? search = null, string? sortBy = null, string? sortOrder = null)
    {
        var query = db.OrganizationMembers
            .Where(m => m.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Organization.Name.ToLower().Contains(search.ToLower()));

        var isDesc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        IQueryable<OrganizationResponse> projected = sortBy?.ToLowerInvariant() switch
        {
            "name" => isDesc
                ? query.OrderByDescending(m => m.Organization.Name).Select(m => new OrganizationResponse(m.Organization.Id, m.Organization.Name, m.Organization.Description, m.Organization.CreatedAt))
                : query.OrderBy(m => m.Organization.Name).Select(m => new OrganizationResponse(m.Organization.Id, m.Organization.Name, m.Organization.Description, m.Organization.CreatedAt)),
            _ => isDesc
                ? query.OrderByDescending(m => m.Organization.CreatedAt).Select(m => new OrganizationResponse(m.Organization.Id, m.Organization.Name, m.Organization.Description, m.Organization.CreatedAt))
                : query.OrderBy(m => m.Organization.CreatedAt).Select(m => new OrganizationResponse(m.Organization.Id, m.Organization.Name, m.Organization.Description, m.Organization.CreatedAt))
        };

        return await projected.ToListAsync();
    }

    public async Task<OrganizationResponse> GetAsync(Guid orgId, Guid userId)
    {
        var org = await db.Organizations.FindAsync(orgId)
                  ?? throw new NotFoundException("Organization not found");

        var isMember = await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.UserId == userId);
        if (!isMember) throw new ForbiddenException();

        return Map(org);
    }

    public async Task<OrganizationResponse> UpdateAsync(Guid orgId, UpdateOrganizationRequest req, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new BadRequestException("Organization name is required");

        var org = await db.Organizations.FindAsync(orgId)
                  ?? throw new NotFoundException("Organization not found");

        await RequireRoleAsync(orgId, userId, OrganizationRole.Owner, OrganizationRole.Admin);

        org.Name = req.Name;
        org.Description = req.Description;
        await db.SaveChangesAsync();

        return Map(org);
    }

    public async Task DeleteAsync(Guid orgId, Guid userId)
    {
        var org = await db.Organizations.FindAsync(orgId)
                  ?? throw new NotFoundException("Organization not found");

        await RequireRoleAsync(orgId, userId, OrganizationRole.Owner);

        db.Organizations.Remove(org);
        await db.SaveChangesAsync();
    }


    public async Task<PagedResponse<MemberResponse>> ListMembersAsync(
        Guid orgId, Guid userId, int page = 1, int pageSize = 20, string? search = null, string? sortBy = null, string? sortOrder = null)
    {
        var isMember = await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.UserId == userId);
        if (!isMember) throw new ForbiddenException();

        var query = db.OrganizationMembers.Where(m => m.OrganizationId == orgId);

        if (!string.IsNullOrWhiteSpace(search) && Enum.TryParse<OrganizationRole>(search, true, out var searchRole))
            query = query.Where(m => m.Role == searchRole);

        var total = await query.CountAsync();

        var isDesc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "role" => isDesc ? query.OrderByDescending(m => m.Role) : query.OrderBy(m => m.Role),
            _ => isDesc ? query.OrderByDescending(m => m.JoinedAt) : query.OrderBy(m => m.JoinedAt)
        };

        var members = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var userIds = members.Select(m => m.UserId).ToList();
        var licenses = await db.Licenses
            .Where(l => l.OrganizationId == orgId && l.AssignedUserId != null && userIds.Contains(l.AssignedUserId!.Value))
            .ToListAsync();

        var items = members.Select(m =>
        {
            var lic = licenses.FirstOrDefault(l => l.AssignedUserId == m.UserId);
            var licInfo = lic == null ? null : new AssignedLicenseInfo(lic.Id, lic.Status.ToString(), lic.ExpiresAt);
            return new MemberResponse(m.UserId, m.Role.ToString(), m.JoinedAt, licInfo);
        }).ToList();

        return new PagedResponse<MemberResponse>(items, total, page, pageSize);
    }

    public async Task<MemberResponse> GetMemberAsync(Guid orgId, Guid targetUserId, Guid callerUserId)
    {
        var isMember = await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.UserId == callerUserId);
        if (!isMember) throw new ForbiddenException();

        var member = await db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == targetUserId)
            ?? throw new NotFoundException("Member not found");

        var lic = await db.Licenses
            .FirstOrDefaultAsync(l => l.OrganizationId == orgId && l.AssignedUserId == targetUserId);

        var licInfo = lic == null ? null : new AssignedLicenseInfo(lic.Id, lic.Status.ToString(), lic.ExpiresAt);
        return new MemberResponse(member.UserId, member.Role.ToString(), member.JoinedAt, licInfo);
    }

    public async Task<MemberResponse> UpdateMemberRoleAsync(Guid orgId, Guid targetUserId, string newRole, Guid callerUserId)
    {
        if (!Enum.TryParse<OrganizationRole>(newRole, true, out var role))
            throw new BadRequestException($"Invalid role: {newRole}");

        var caller = await db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == callerUserId)
            ?? throw new ForbiddenException();

        if (caller.Role == OrganizationRole.Member) throw new ForbiddenException();

        if (caller.Role == OrganizationRole.Admin && role != OrganizationRole.Member)
            throw new ForbiddenException("Admins can only assign the Member role");

        var currentTarget = await db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == targetUserId)
            ?? throw new NotFoundException("Member not found");

        if (caller.Role == OrganizationRole.Admin && currentTarget.Role == OrganizationRole.Owner)
            throw new ForbiddenException("Admins cannot change an Owner's role");

        if (callerUserId == targetUserId && caller.Role == OrganizationRole.Owner && role != OrganizationRole.Owner)
        {
            var ownerCount = await db.OrganizationMembers
                .CountAsync(m => m.OrganizationId == orgId && m.Role == OrganizationRole.Owner);
            if (ownerCount == 1)
                throw new BadRequestException("Cannot demote the sole owner of an organization");
        }

        currentTarget.Role = role;
        await db.SaveChangesAsync();

        return new MemberResponse(currentTarget.UserId, currentTarget.Role.ToString(), currentTarget.JoinedAt, null);
    }

    public async Task RemoveMemberAsync(Guid orgId, Guid targetUserId, Guid callerUserId)
    {
        var caller = await db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == callerUserId)
            ?? throw new ForbiddenException();

        if (callerUserId == targetUserId)
        {
            if (caller.Role == OrganizationRole.Owner)
            {
                var ownerCount = await db.OrganizationMembers
                    .CountAsync(m => m.OrganizationId == orgId && m.Role == OrganizationRole.Owner);
                if (ownerCount == 1)
                    throw new BadRequestException("Cannot leave as the sole owner. Transfer ownership first.");
            }
            db.OrganizationMembers.Remove(caller);
            await db.SaveChangesAsync();
            return;
        }

        if (caller.Role == OrganizationRole.Member) throw new ForbiddenException();

        var target = await db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == targetUserId)
            ?? throw new NotFoundException("Member not found");

        if (caller.Role == OrganizationRole.Admin && target.Role == OrganizationRole.Owner)
            throw new ForbiddenException("Admins cannot remove an Owner");

        db.OrganizationMembers.Remove(target);
        await db.SaveChangesAsync();
    }


    public async Task AssignLicenseAsync(Guid orgId, Guid targetUserId, Guid callerUserId)
    {
        await RequireRoleAsync(orgId, callerUserId, OrganizationRole.Owner, OrganizationRole.Admin);

        var isMember = await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.UserId == targetUserId);
        if (!isMember) throw new NotFoundException("Target user is not a member of this organization");

        var alreadyAssigned = await db.Licenses.AnyAsync(l => l.OrganizationId == orgId && l.AssignedUserId == targetUserId);
        if (alreadyAssigned) throw new BadRequestException("User already has a license assigned");

        var license = await db.Licenses
            .FirstOrDefaultAsync(l => l.OrganizationId == orgId && l.AssignedUserId == null && l.Status == LicenseStatus.Active)
            ?? throw new NotFoundException("No available active license for this organization");

        license.AssignedUserId = targetUserId;
        await db.SaveChangesAsync();
    }

    public async Task UnassignLicenseAsync(Guid orgId, Guid targetUserId, Guid callerUserId)
    {
        await RequireRoleAsync(orgId, callerUserId, OrganizationRole.Owner, OrganizationRole.Admin);

        var license = await db.Licenses
            .FirstOrDefaultAsync(l => l.OrganizationId == orgId && l.AssignedUserId == targetUserId)
            ?? throw new NotFoundException("No license assigned to this user in this organization");

        license.AssignedUserId = null;
        await db.SaveChangesAsync();
    }


    private async Task RequireRoleAsync(Guid orgId, Guid userId, params OrganizationRole[] allowedRoles)
    {
        var member = await db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId)
            ?? throw new ForbiddenException();

        if (!allowedRoles.Contains(member.Role)) throw new ForbiddenException();
    }

    private static OrganizationResponse Map(Organization org)
        => new(org.Id, org.Name, org.Description, org.CreatedAt);
}
