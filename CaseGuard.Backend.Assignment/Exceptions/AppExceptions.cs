namespace CaseGuard.Backend.Assignment.Exceptions;

public class NotFoundException(string message = "Not found") : Exception(message);
public class ForbiddenException(string message = "Forbidden") : Exception(message);
public class UnauthorizedException(string message = "Unauthorized") : Exception(message);
public class BadRequestException(string message) : Exception(message);
