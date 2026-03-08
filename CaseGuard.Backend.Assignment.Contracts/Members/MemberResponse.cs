namespace CaseGuard.Backend.Assignment.Contracts.Members;

public record MemberResponse(Guid UserId, string Role, DateTime JoinedAt, AssignedLicenseInfo? AssignedLicense);
public record AssignedLicenseInfo(Guid LicenseId, string Status, DateTime ExpiresAt);
