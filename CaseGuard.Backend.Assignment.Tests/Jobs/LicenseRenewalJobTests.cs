using CaseGuard.Backend.Assignment.Data;
using CaseGuard.Backend.Assignment.Jobs;
using CaseGuard.Backend.Assignment.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CaseGuard.Backend.Assignment.Tests.Jobs;

public class LicenseRenewalJobTests
{
    private static (LicenseRenewalJob job, IServiceScopeFactory scopeFactory) CreateJob(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var job = new LicenseRenewalJob(scopeFactory, NullLogger<LicenseRenewalJob>.Instance);
        return (job, scopeFactory);
    }

    private static async Task SeedAsync(IServiceScopeFactory scopeFactory, params License[] licenses)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Licenses.AddRange(licenses);
        await db.SaveChangesAsync();
    }

    private static async Task<License> ReloadAsync(IServiceScopeFactory scopeFactory, Guid id)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (await db.Licenses.FindAsync(id))!;
    }

    private static Organization MakeOrg() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Company 1",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task ExpiredLicense_MarkedAsExpired()
    {
        var (job, scopeFactory) = CreateJob(nameof(ExpiredLicense_MarkedAsExpired));
        var org = MakeOrg();
        var license = new License
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Organization = org,
            Status = LicenseStatus.Active,
            AutoRenew = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        };
        await SeedAsync(scopeFactory, license);

        await job.ProcessAsync(CancellationToken.None);

        var result = await ReloadAsync(scopeFactory, license.Id);
        Assert.Equal(LicenseStatus.Expired, result.Status);
    }

    [Fact]
    public async Task AutoRenewLicense_GetsRenewed()
    {
        var (job, scopeFactory) = CreateJob(nameof(AutoRenewLicense_GetsRenewed));
        var org = MakeOrg();
        var originalExpiry = DateTime.UtcNow.AddMinutes(1);
        var license = new License
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Organization = org,
            Status = LicenseStatus.Active,
            AutoRenew = true,
            ExpiresAt = originalExpiry,
            CreatedAt = DateTime.UtcNow.AddMinutes(-9)
        };
        await SeedAsync(scopeFactory, license);

        await job.ProcessAsync(CancellationToken.None);

        var result = await ReloadAsync(scopeFactory, license.Id);
        Assert.Equal(LicenseStatus.Active, result.Status);
        Assert.True(result.ExpiresAt > originalExpiry);
        Assert.NotNull(result.RenewedAt);
    }

    [Fact]
    public async Task MixedLicenses_ProcessedCorrectly()
    {
        var (job, scopeFactory) = CreateJob(nameof(MixedLicenses_ProcessedCorrectly));
        var org = MakeOrg();

        var toExpire = new License
        {
            Id = Guid.NewGuid(), OrganizationId = org.Id, Organization = org,
            Status = LicenseStatus.Active, AutoRenew = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), CreatedAt = DateTime.UtcNow.AddMinutes(-11)
        };
        var toRenew = new License
        {
            Id = Guid.NewGuid(), OrganizationId = org.Id, Organization = org,
            Status = LicenseStatus.Active, AutoRenew = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(1), CreatedAt = DateTime.UtcNow.AddMinutes(-9)
        };
        var untouched = new License
        {
            Id = Guid.NewGuid(), OrganizationId = org.Id, Organization = org,
            Status = LicenseStatus.Active, AutoRenew = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(8), CreatedAt = DateTime.UtcNow
        };

        await SeedAsync(scopeFactory, toExpire, toRenew, untouched);

        await job.ProcessAsync(CancellationToken.None);

        var r1 = await ReloadAsync(scopeFactory, toExpire.Id);
        var r2 = await ReloadAsync(scopeFactory, toRenew.Id);
        var r3 = await ReloadAsync(scopeFactory, untouched.Id);

        Assert.Equal(LicenseStatus.Expired, r1.Status);
        Assert.Equal(LicenseStatus.Active, r2.Status);
        Assert.NotNull(r2.RenewedAt);
        Assert.Equal(LicenseStatus.Active, r3.Status);
        Assert.Null(r3.RenewedAt);
    }
}
