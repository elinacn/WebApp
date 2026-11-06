using Microsoft.EntityFrameworkCore;

namespace WebApp.Models
{
    public class SheetsDbContext : DbContext
    {
        public SheetsDbContext(DbContextOptions<SheetsDbContext> options) : base(options)
        {
        }

        // For the Streaks
        public DbSet<DailyStreaks> DailyStreaks { get; set; }

        // For creating quests
        public DbSet<Quest> Quests { get; set; }
        public DbSet<QuestQuestion> QuestQuestions { get; set; }
        public DbSet<QuestOption> QuestOptions { get; set; }

        // For user management
        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }

        // Points earned from completed quests
        public DbSet<QuestCompletion> QuestCompletions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enforce unique titles at the database level too
        modelBuilder.Entity<Quest>().HasIndex(q => q.Title).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Course>().HasIndex(c => c.Code).IsUnique();
        modelBuilder.Entity<DailyStreaks>().HasIndex(a => new { a.UserId, a.ActivityDate }).IsUnique();

        // Deleting a course unlinks its quests instead of deleting them
        modelBuilder.Entity<Quest>()
            .HasOne(q => q.Course).WithMany(c => c.Quests)
            .HasForeignKey(q => q.CourseId).OnDelete(DeleteBehavior.SetNull);

        // One completion row per user per quest; removed with the user or the quest
        modelBuilder.Entity<QuestCompletion>().HasIndex(c => new { c.UserId, c.QuestId }).IsUnique();
        modelBuilder.Entity<QuestCompletion>()
            .HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<QuestCompletion>()
            .HasOne(c => c.Quest).WithMany()
            .HasForeignKey(c => c.QuestId).OnDelete(DeleteBehavior.Cascade);
    }
    }
}