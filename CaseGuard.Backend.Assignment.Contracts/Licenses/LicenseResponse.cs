namespace CaseGuard.Backend.Assignment.Contracts.Licenses;

public record LicenseResponse(Guid Id, Guid OrganizationId, Guid? AssignedUserId, string Status, bool AutoRenew, DateTime ExpiresAt, DateTime CreatedAt, DateTime? RenewedAt);
