namespace CaseGuard.Backend.Assignment.Models;

public class License
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public LicenseStatus Status { get; set; }
    public bool AutoRenew { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RenewedAt { get; set; }

    public Organization Organization { get; set; } = default!;
}
