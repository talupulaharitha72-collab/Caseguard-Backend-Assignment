namespace CaseGuard.Backend.Assignment.Contracts.Licenses;

public record UpdateLicenseRequest(DateTime? ExpiresAt, bool? AutoRenew, string? Status);
