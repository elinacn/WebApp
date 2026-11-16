using Microsoft.EntityFrameworkCore;
using WebApp.Api.Data;
using WebApp.Api.Models;

namespace WebApp.Api.Services;

public record CompletionResult(int XpAwarded, bool FirstCompletion, int BestScore, int TotalXp);

// The XP rule: completing a quest the first time awards XP based on its difficulty
// (Beginner 10, Intermediate 25, Expert 40, Master 60). Retakes never award XP again,
// but still update the best score and the attempt count.
public class QuestScoringService
{
    private readonly AppDbContext _db;
    private readonly ILogger<QuestScoringService> _logger;

    public QuestScoringService(AppDbContext db, ILogger<QuestScoringService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CompletionResult> RecordCompletionAsync(int userId, Quest quest, int score, int maxScore)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} does not exist.");

        var completion = await _db.QuestCompletions
            .FirstOrDefaultAsync(c => c.UserId == userId && c.QuestId == quest.Id);
        var now = DateTime.UtcNow;
        var firstCompletion = completion is null;

        if (completion is null)
        {
            completion = new QuestCompletion { UserId = userId, QuestId = quest.Id, FirstCompletedAt = now };
            _db.QuestCompletions.Add(completion);
            user.QuizzesCompleted++;
        }

        var xp = firstCompletion ? DifficultyRules.GetCompletionXp(quest.Difficulty) : 0;
        completion.BestScore = Math.Max(completion.BestScore, score);
        completion.MaxScore = maxScore;
        completion.Attempts++;
        completion.LastCompletedAt = now;
        user.Points += xp;

        // Saves the completion and the user's new XP together, or neither
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} completed quest {QuestId}: {Score}/{MaxScore}, +{Xp} XP (attempt {Attempt})",
            userId, quest.Id, score, maxScore, xp, completion.Attempts);

        return new CompletionResult(xp, firstCompletion, completion.BestScore, user.Points);
    }
}
