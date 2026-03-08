namespace CaseGuard.Backend.Assignment.Contracts.Invitations;

public record InvitationResponse(Guid Id, Guid OrganizationId, string InviteeEmail, Guid InvitedByUserId, string Status, DateTime CreatedAt, DateTime ExpiresAt);
