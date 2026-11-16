using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WebApp.Api.Models;

namespace WebApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Quest> Quests => Set<Quest>();
    public DbSet<QuestQuestion> QuestQuestions => Set<QuestQuestion>();
    public DbSet<QuestOption> QuestOptions => Set<QuestOption>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<User> Users => Set<User>();
    public DbSet<QuestCompletion> QuestCompletions => Set<QuestCompletion>();
    public DbSet<DailyActivity> DailyActivities => Set<DailyActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Unique values, enforced by the database as well as by the controllers
        modelBuilder.Entity<Quest>().HasIndex(q => q.Title).IsUnique();
        modelBuilder.Entity<Course>().HasIndex(c => c.Code).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        // Deleting a course unlinks its quests instead of deleting them
        modelBuilder.Entity<Quest>()
            .HasOne(q => q.Course).WithMany(c => c.Quests)
            .HasForeignKey(q => q.CourseId).OnDelete(DeleteBehavior.SetNull);

        // A quest owns its questions, and a question owns its options
        modelBuilder.Entity<QuestQuestion>()
            .HasOne<Quest>().WithMany(q => q.Questions)
            .HasForeignKey(q => q.QuestId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<QuestOption>()
            .HasOne<QuestQuestion>().WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestQuestionId).OnDelete(DeleteBehavior.Cascade);

        // User <-> Quest many-to-many: one completion per user per quest,
        // removed together with the user or the quest
        modelBuilder.Entity<QuestCompletion>().HasIndex(c => new { c.UserId, c.QuestId }).IsUnique();
        modelBuilder.Entity<QuestCompletion>()
            .HasOne(c => c.User).WithMany(u => u.Completions)
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<QuestCompletion>()
            .HasOne(c => c.Quest).WithMany(q => q.Completions)
            .HasForeignKey(c => c.QuestId).OnDelete(DeleteBehavior.Cascade);

        // One activity row per user per day
        modelBuilder.Entity<DailyActivity>().HasIndex(a => new { a.UserId, a.Date }).IsUnique();
        modelBuilder.Entity<DailyActivity>()
            .HasOne(a => a.User).WithMany()
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    // All dates are saved in UTC. SQLite doesn't store that, so mark them as UTC when
    // reading; the JSON then ends in "Z" and the browser shows them in local time.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
