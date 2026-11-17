using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApp.Models;
using WebApp.DAL;
using WebApp.Services;

namespace WebApp.Tests;

public class DifficultyXpTests
{
    [Theory]
    [InlineData("Beginner", 10)]
    [InlineData("Intermediate", 25)]
    [InlineData("Expert", 40)]
    [InlineData("Master", 60)]
    public async Task Completion_awards_difficulty_xp_once_while_retakes_update_best_score(string difficulty, int reward)
    {
        var options = new DbContextOptionsBuilder<SheetsDbContext>().UseSqlite("Data Source=:memory:").Options;
        await using var db = new SheetsDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        var user = new User { Username = "student", Name = "Student", StudyProgram = "Web" };
        var quest = new Quest { Title = "Reward test", Difficulty = difficulty };
        db.AddRange(user, quest);
        await db.SaveChangesAsync();
        var service = new QuestScoringService(new UserRepository(db), new QuestRepository(db), new ProgressRepository(db), NullLogger<QuestScoringService>.Instance);

        var first = await service.RecordCompletionAsync(user.Id, quest.Id, 0, 10);
        Assert.Equal(reward, first.PointsAwarded);
        Assert.Equal(reward, first.TotalPoints);
        Assert.True(first.FirstCompletion);

        var improved = await service.RecordCompletionAsync(user.Id, quest.Id, 10, 10);
        Assert.Equal(0, improved.PointsAwarded);
        Assert.Equal(reward, improved.TotalPoints);
        Assert.Equal(10, improved.BestScore);
        Assert.False(improved.FirstCompletion);
        Assert.Equal(1, user.QuizzesCompleted);
        Assert.Equal(2, (await db.QuestCompletions.SingleAsync()).Attempts);

        // Completing a different quest still awards its own reward.
        var another = new Quest { Title = "Another reward test", Difficulty = difficulty };
        db.Quests.Add(another);
        await db.SaveChangesAsync();
        await service.RecordCompletionAsync(user.Id, another.Id, 5, 10);
        Assert.Equal(reward * 2, user.Points);
        Assert.Equal(2, user.QuizzesCompleted);
    }

    [Theory]
    [InlineData("apprentice", "Beginner", 10)]
    [InlineData("knight", "Intermediate", 25)]
    public void Legacy_difficulties_use_the_new_names_and_rewards(string oldName, string newName, int xp)
    {
        Assert.Equal(newName, DifficultyRules.Normalize(oldName));
        Assert.Equal(xp, DifficultyRules.GetCompletionXp(oldName));
    }
}
