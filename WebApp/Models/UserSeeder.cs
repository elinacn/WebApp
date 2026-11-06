using Microsoft.AspNetCore.Identity;

namespace WebApp.Models;

// Demo students. Only inserted when the Users table is empty, so users added or
// deleted through the site are never overwritten. They start with 0 code points;
// points come only from completing quests (see QuestScoringService).
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
                new User { Username = "anna", Name = "Anna", StudyProgram = "Web programming", CreatedAt = today },
                new User { Username = "beatrice", Name = "Beatrice", StudyProgram = "Web programming", CreatedAt = today },
                new User { Username = "calvin", Name = "Calvin", StudyProgram = "Programming basics", CreatedAt = today },
                new User { Username = "elina", Name = "Elina", StudyProgram = "Web programming", CreatedAt = today },
                new User { Username = "froed", Name = "Froed", StudyProgram = "Programming basics", CreatedAt = today },
                new User { Username = "johan", Name = "Johan", StudyProgram = "Web programming", CreatedAt = today },
                new User { Username = "oyvind", Name = "Øyvind", StudyProgram = "Programming basics", CreatedAt = today },
                new User { Username = "maya", Name = "Maya", StudyProgram = "Web programming", CreatedAt = today }
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
