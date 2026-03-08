using System.Text.Json;
using CaseGuard.Backend.Assignment.Exceptions;

namespace CaseGuard.Backend.Assignment.Middleware;

public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            ForbiddenException e => (StatusCodes.Status403Forbidden, e.Message),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, e.Message),
            BadRequestException e => (StatusCodes.Status400BadRequest, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == 500)
            logger.LogError(ex, "Unhandled exception");

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(body);
    }
}
