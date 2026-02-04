using FairPlay.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FairPlay.Api.Data;

public class FairPlayDbContext : IdentityDbContext<ApplicationUser>
{
    public FairPlayDbContext(DbContextOptions<FairPlayDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchAssignment> MatchAssignments => Set<MatchAssignment>();
    public DbSet<RawRating> RawRatings => Set<RawRating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite key for MatchAssignment
        modelBuilder.Entity<MatchAssignment>()
            .HasKey(ma => new { ma.MatchId, ma.PlayerId });

        // Relationship configuration
        modelBuilder.Entity<RawRating>()
            .HasOne(r => r.Rater)
            .WithMany()
            .HasForeignKey(r => r.RaterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RawRating>()
            .HasOne(r => r.Subject)
            .WithMany()
            .HasForeignKey(r => r.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
