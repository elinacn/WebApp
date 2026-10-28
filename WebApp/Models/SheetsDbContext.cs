using Microsoft.EntityFrameworkCore;

namespace WebApp.Models
{
    public class SheetsDbContext : DbContext
    {
        public SheetsDbContext(DbContextOptions<SheetsDbContext> options) : base(options)
        {
        }

        // For creating quests
        public DbSet<Quest> Quests { get; set; }
        public DbSet<QuestQuestion> QuestQuestions { get; set; }
        public DbSet<QuestOption> QuestOptions { get; set; }

        // For user management
        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enforce unique titles at the database level too
        modelBuilder.Entity<Quest>().HasIndex(q => q.Title).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Course>().HasIndex(c => c.Code).IsUnique();

        // Deleting a course unlinks its quests instead of deleting them
        modelBuilder.Entity<Quest>()
            .HasOne(q => q.Course).WithMany(c => c.Quests)
            .HasForeignKey(q => q.CourseId).OnDelete(DeleteBehavior.SetNull);
    }
    }
}