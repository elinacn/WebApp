using AspNetCoreGeneratedDocument;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Models;

// DB context for tracking leaderboard scores and attempts.
public class LeaderboardDbContext : DbContext
{
    public LeaderboardDbContext(DbContextOptions<LeaderboardDbContext> options) : base(options)
    {
    }

    // QuestAttempts table in DB
    public DbSet<QuestAttempt> QuestAttempts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // adds an index on StudentName and Course to make search and ranking queries faster
        modelBuilder.Entity<QuestAttempt>()
            .HasIndex(q => new { q.StudentName });
    }
}