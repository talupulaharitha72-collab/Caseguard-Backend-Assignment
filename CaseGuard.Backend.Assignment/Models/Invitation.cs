namespace CaseGuard.Backend.Assignment.Models;

public class Invitation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string InviteeEmail { get; set; } = default!;
    public Guid InvitedByUserId { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public Organization Organization { get; set; } = default!;
}
