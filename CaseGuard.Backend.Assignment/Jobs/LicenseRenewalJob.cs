using CaseGuard.Backend.Assignment.Data;
using CaseGuard.Backend.Assignment.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseGuard.Backend.Assignment.Jobs;

public class LicenseRenewalJob(IServiceScopeFactory scopeFactory, ILogger<LicenseRenewalJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }

    internal async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        var toExpire = await db.Licenses
            .Where(l => l.Status == LicenseStatus.Active && !l.AutoRenew && l.ExpiresAt <= now)
            .ToListAsync(ct);

        foreach (var l in toExpire)
            l.Status = LicenseStatus.Expired;

        var threshold = now.AddMinutes(2);
        var toRenew = await db.Licenses
            .Where(l => l.AutoRenew && l.Status == LicenseStatus.Active && l.ExpiresAt <= threshold)
            .ToListAsync(ct);

        foreach (var l in toRenew)
        {
            l.ExpiresAt = now.AddMinutes(10);
            l.RenewedAt = now;
        }

        if (toExpire.Count > 0 || toRenew.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            if (toExpire.Count > 0) logger.LogInformation("Expired {Count} licenses", toExpire.Count);
            if (toRenew.Count > 0) logger.LogInformation("Auto-renewed {Count} licenses", toRenew.Count);
        }
    }
}
