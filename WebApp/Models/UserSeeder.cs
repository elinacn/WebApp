using Microsoft.AspNetCore.Identity;

namespace WebApp.Models;

// Demo students for the leaderboard. Only inserted when the Users table is empty,
// so users added or deleted through the site are never overwritten.
public static class UserSeeder
{
    // Shared password for the demo users, so the team can sign in as any of them.
    // Shown on the sign-in page in Development only.
    public const string DemoPassword = "quest1234";

    public static void Seed(SheetsDbContext context, ILogger logger)
    {
        var hasher = new PasswordHasher<User>();

        if (!context.Users.Any())
        {
            var today = DateTime.Today;
            context.Users.AddRange(
                new User { Username = "anna", Name = "Anna", StudyProgram = "Web programming", Points = 3000, QuizzesCompleted = 24, CreatedAt = today },
                new User { Username = "beatrice", Name = "Beatrice", StudyProgram = "Web programming", Points = 2400, QuizzesCompleted = 20, CreatedAt = today },
                new User { Username = "calvin", Name = "Calvin", StudyProgram = "Programming basics", Points = 2350, QuizzesCompleted = 19, CreatedAt = today },
                new User { Username = "elina", Name = "Elina", StudyProgram = "Web programming", Points = 2100, QuizzesCompleted = 17, CreatedAt = today },
                new User { Username = "froed", Name = "Froed", StudyProgram = "Programming basics", Points = 1800, QuizzesCompleted = 15, CreatedAt = today },
                new User { Username = "johan", Name = "Johan", StudyProgram = "Web programming", Points = 1250, QuizzesCompleted = 12, CreatedAt = today },
                new User { Username = "oyvind", Name = "Øyvind", StudyProgram = "Programming basics", Points = 1100, QuizzesCompleted = 10, CreatedAt = today },
                new User { Username = "maya", Name = "Maya", StudyProgram = "Web programming", Points = 850, QuizzesCompleted = 8, CreatedAt = today }
            );
            context.SaveChanges();
            logger.LogInformation("Seeded {Count} demo users", 8);
        }

        // Demo users created before passwords existed get the demo password once.
        // Other users without a password are left alone: they get one through the Users page.
        var demoUsernames = new[] { "anna", "beatrice", "calvin", "elina", "froed", "johan", "oyvind", "maya" };
        var withoutPassword = context.Users
            .Where(u => u.PasswordHash == null && demoUsernames.Contains(u.Username))
            .ToList();
        foreach (var user in withoutPassword)
        {
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
        }
        if (withoutPassword.Count > 0)
        {
            context.SaveChanges();
            logger.LogInformation("Set the demo password for {Count} demo users", withoutPassword.Count);
        }
    }
}
