namespace CaseGuard.Backend.Assignment.Contracts.Auth;

public record LoginResponse(string Token, Guid UserId, string Email, string Role);
