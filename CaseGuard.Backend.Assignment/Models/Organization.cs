namespace CaseGuard.Backend.Assignment.Models;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    public ICollection<License> Licenses { get; set; } = new List<License>();
}
