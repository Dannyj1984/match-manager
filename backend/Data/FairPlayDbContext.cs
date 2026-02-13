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
    public DbSet<League> Leagues => Set<League>();
    public DbSet<LeagueMembership> LeagueMemberships => Set<LeagueMembership>();
    public DbSet<PlayerRating> PlayerRatings => Set<PlayerRating>();
    public DbSet<LeagueJoinRequest> LeagueJoinRequests => Set<LeagueJoinRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite key for MatchAssignment
        modelBuilder.Entity<MatchAssignment>()
            .HasKey(ma => new { ma.MatchId, ma.PlayerId });

        // Unique constraint for LeagueMembership (one user can only have one membership per league)
        modelBuilder.Entity<LeagueMembership>()
            .HasIndex(lm => new { lm.LeagueId, lm.UserId })
            .IsUnique();

        // Relationship configuration
        // Unique index for join requests (one pending request per user per league)
        modelBuilder.Entity<LeagueJoinRequest>()
            .HasIndex(jr => new { jr.LeagueId, jr.UserId })
            .HasFilter("\"Status\" = 'Pending'")
            .IsUnique();

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
