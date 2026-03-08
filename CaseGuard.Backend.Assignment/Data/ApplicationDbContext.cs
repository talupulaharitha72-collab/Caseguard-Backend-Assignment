using CaseGuard.Backend.Assignment.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseGuard.Backend.Assignment.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<License> Licenses => Set<License>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<OrganizationMember>()
            .HasKey(m => new { m.OrganizationId, m.UserId });

        mb.Entity<Organization>()
            .HasMany(o => o.Members)
            .WithOne(m => m.Organization)
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Organization>()
            .HasMany(o => o.Invitations)
            .WithOne(i => i.Organization)
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Organization>()
            .HasMany(o => o.Licenses)
            .WithOne(l => l.Organization)
            .HasForeignKey(l => l.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<License>().HasIndex(l => l.Status);
        mb.Entity<License>().HasIndex(l => l.ExpiresAt);
        mb.Entity<Invitation>().HasIndex(i => i.InviteeEmail);
    }
}
