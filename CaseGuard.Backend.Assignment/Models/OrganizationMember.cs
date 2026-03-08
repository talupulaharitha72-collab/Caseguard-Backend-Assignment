namespace CaseGuard.Backend.Assignment.Models;

public class OrganizationMember
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public OrganizationRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public Organization Organization { get; set; } = default!;
}
