namespace CaseGuard.Backend.Assignment.Contracts.Organizations;

public record OrganizationResponse(Guid Id, string Name, string? Description, DateTime CreatedAt);
