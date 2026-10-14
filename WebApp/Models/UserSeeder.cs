namespace WebApp.Models;

// Demo students for the leaderboard. Only inserted when the Users table is empty,
// so users added or deleted through the site are never overwritten.
public static class UserSeeder
{
    public static void Seed(SheetsDbContext context, ILogger logger)
    {
        if (context.Users.Any()) return;

        var today = DateTime.Today;
        context.Users.AddRange(
            new User { Username = "anna", Name = "Anna", Course = "Web programming", Points = 3000, QuizzesCompleted = 24, CreatedAt = today },
            new User { Username = "beatrice", Name = "Beatrice", Course = "Web programming", Points = 2400, QuizzesCompleted = 20, CreatedAt = today },
            new User { Username = "calvin", Name = "Calvin", Course = "Programming basics", Points = 2350, QuizzesCompleted = 19, CreatedAt = today },
            new User { Username = "elina", Name = "Elina", Course = "Web programming", Points = 2100, QuizzesCompleted = 17, CreatedAt = today },
            new User { Username = "froed", Name = "Froed", Course = "Programming basics", Points = 1800, QuizzesCompleted = 15, CreatedAt = today },
            new User { Username = "johan", Name = "Johan", Course = "Web programming", Points = 1250, QuizzesCompleted = 12, CreatedAt = today },
            new User { Username = "oyvind", Name = "Øyvind", Course = "Programming basics", Points = 1100, QuizzesCompleted = 10, CreatedAt = today },
            new User { Username = "maya", Name = "Maya", Course = "Web programming", Points = 850, QuizzesCompleted = 8, CreatedAt = today }
        );
        context.SaveChanges();
        logger.LogInformation("Seeded {Count} demo users", 8);
    }
}
