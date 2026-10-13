using Microsoft.EntityFrameworkCore;

namespace WebApp.Models
{
    public class SheetsDbContext : DbContext
    {
        public SheetsDbContext(DbContextOptions<SheetsDbContext> options) : base(options)
        {
        }
        public DbSet<Sheet> Sheets { get; set; }

        // For creating quests
        public DbSet<Quest> Quests { get; set; }
        public DbSet<QuestQuestion> QuestQuestions { get; set; }
        public DbSet<QuestOption> QuestOptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enforce unique titles at the database level too
        modelBuilder.Entity<Sheet>().HasIndex(s => s.Title).IsUnique();
        modelBuilder.Entity<Quest>().HasIndex(q => q.Title).IsUnique();
    }
    }
}