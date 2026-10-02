using Microsoft.EntityFrameworkCore;

namespace WebApp.Models
{
    public class SheetsDbContext : DbContext
    {
        public SheetsDbContext(DbContextOptions<SheetsDbContext> options) : base(options)
        {
        }
        public DbSet<Sheet> Sheets { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enforce unique titles at the database level too
        modelBuilder.Entity<Sheet>().HasIndex(s => s.Title).IsUnique();
    }
    }
}