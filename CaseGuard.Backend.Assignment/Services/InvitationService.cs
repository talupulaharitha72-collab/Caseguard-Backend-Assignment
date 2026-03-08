using System.Security.Cryptography;
using System.Text;
using CaseGuard.Backend.Assignment.Contracts.Invitations;
using CaseGuard.Backend.Assignment.Data;
using CaseGuard.Backend.Assignment.Exceptions;
using CaseGuard.Backend.Assignment.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseGuard.Backend.Assignment.Services;

public class InvitationService(ApplicationDbContext db)
{
    public async Task<InvitationResponse> CreateAsync(Guid orgId, CreateInvitationRequest req, Guid callerUserId)
    {
        await RequireOwnerOrAdminAsync(orgId, callerUserId);

        if (string.IsNullOrWhiteSpace(req.InviteeEmail) || !req.InviteeEmail.Contains('@'))
            throw new BadRequestException("A valid email address is required");

        var orgExists = await db.Organizations.AnyAsync(o => o.Id == orgId);
        if (!orgExists) throw new NotFoundException("Organization not found");

        var inviteeUserId = DeterministicGuid(req.InviteeEmail);
        var isMember = await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.UserId == inviteeUserId);
        if (isMember) throw new BadRequestException("This user is already a member of the organization");

        var hasPending = await db.Invitations.AnyAsync(i =>
            i.OrganizationId == orgId &&
            i.InviteeEmail.ToLower() == req.InviteeEmail.ToLower() &&
            i.Status == InvitationStatus.Pending);
        if (hasPending) throw new BadRequestException("A pending invitation already exists for this email");

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            InviteeEmail = req.InviteeEmail.ToLower(),
            InvitedByUserId = callerUserId,
            Status = InvitationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        db.Invitations.Add(invitation);
        await db.SaveChangesAsync();

        return Map(invitation);
    }

    public async Task<List<InvitationResponse>> ListAsync(Guid orgId, Guid callerUserId, string? status, string? search = null, string? sortOrder = null)
    {
        await RequireOwnerOrAdminAsync(orgId, callerUserId);

        var query = db.Invitations.Where(i => i.OrganizationId == orgId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvitationStatus>(status, true, out var parsedStatus))
            query = query.Where(i => i.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => i.InviteeEmail.ToLower().Contains(search.ToLower()));

        var isDesc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        query = isDesc ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt);

        return await query.Select(i => Map(i)).ToListAsync();
    }

    public async Task<InvitationResponse> GetAsync(Guid orgId, Guid invId, Guid callerUserId)
    {
        await RequireOwnerOrAdminAsync(orgId, callerUserId);

        var inv = await db.Invitations.FirstOrDefaultAsync(i => i.Id == invId && i.OrganizationId == orgId)
                  ?? throw new NotFoundException("Invitation not found");

        return Map(inv);
    }

    public async Task CancelAsync(Guid orgId, Guid invId, Guid callerUserId)
    {
        await RequireOwnerOrAdminAsync(orgId, callerUserId);

        var inv = await db.Invitations.FirstOrDefaultAsync(i => i.Id == invId && i.OrganizationId == orgId)
                  ?? throw new NotFoundException("Invitation not found");

        if (inv.Status != InvitationStatus.Pending)
            throw new BadRequestException("Only pending invitations can be cancelled");

        inv.Status = InvitationStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public async Task AcceptAsync(Guid invId, Guid callerUserId, string callerEmail)
    {
        var inv = await db.Invitations.FindAsync(invId)
                  ?? throw new NotFoundException("Invitation not found");

        if (!string.Equals(inv.InviteeEmail, callerEmail, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("This invitation is not for your email address");

        if (inv.Status != InvitationStatus.Pending)
            throw new BadRequestException("Invitation is no longer pending");

        if (inv.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Invitation has expired");

        var alreadyMember = await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == inv.OrganizationId && m.UserId == callerUserId);
        if (alreadyMember) throw new BadRequestException("You are already a member of this organization");

        db.OrganizationMembers.Add(new OrganizationMember
        {
            OrganizationId = inv.OrganizationId,
            UserId = callerUserId,
            Role = OrganizationRole.Member,
            JoinedAt = DateTime.UtcNow
        });

        inv.Status = InvitationStatus.Accepted;
        await db.SaveChangesAsync();
    }

    private static Guid DeterministicGuid(string email)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
        return new Guid(bytes);
    }

    private async Task RequireOwnerOrAdminAsync(Guid orgId, Guid userId)
    {
        var member = await db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId)
            ?? throw new ForbiddenException();

        if (member.Role == OrganizationRole.Member) throw new ForbiddenException();
    }

    private static InvitationResponse Map(Invitation i)
        => new(i.Id, i.OrganizationId, i.InviteeEmail, i.InvitedByUserId, i.Status.ToString(), i.CreatedAt, i.ExpiresAt);
}
