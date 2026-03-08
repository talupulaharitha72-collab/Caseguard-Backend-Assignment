namespace CaseGuard.Backend.Assignment.Contracts.Licenses;

public record CreateLicenseRequest(Guid OrganizationId, bool AutoRenew);
