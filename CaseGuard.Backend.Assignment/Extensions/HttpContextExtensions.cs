using System.Security.Claims;
using CaseGuard.Backend.Assignment.Exceptions;

namespace CaseGuard.Backend.Assignment.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetCurrentUserId(this HttpContext ctx)
    {
        var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? ctx.User.FindFirstValue("sub");
        if (sub == null) throw new UnauthorizedException();
        return Guid.Parse(sub);
    }

    public static string GetCurrentUserEmail(this HttpContext ctx)
        => ctx.User.FindFirstValue(ClaimTypes.Email)
           ?? ctx.User.FindFirstValue("email")
           ?? throw new UnauthorizedException();
}
