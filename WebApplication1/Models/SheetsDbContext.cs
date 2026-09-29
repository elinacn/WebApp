using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class SheetsDbContext : DbContext
    {
        public SheetsDbContext(DbContextOptions<SheetsDbContext> options) : base(options)
        {
            Database.EnsureCreated(); // Early prototyping, remove later
        }

        public DbSet<Sheet> Sheets { get; set; }
    }
}