using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Services;

// The outcome of one completed attempt, shown on the Play page after submitting
public record QuestResult(int Score, int MaxScore, int PointsAwarded, int BestScore, bool FirstCompletion, int TotalPoints);

// Awards difficulty-based XP once per user and quest. Retakes still update the
// best answer score, attempt count and recent activity, without awarding more XP.
// User.Points is the shared XP total used by the profile, home page and leaderboard.
public class QuestScoringService
{
    private readonly SheetsDbContext _context;
    private readonly ILogger<QuestScoringService> _logger;

    public QuestScoringService(SheetsDbContext context, ILogger<QuestScoringService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QuestResult> RecordCompletionAsync(int userId, int questId, int score, int maxScore)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} does not exist.");

        var quest = await _context.Quests.FindAsync(questId)
            ?? throw new InvalidOperationException($"Quest {questId} does not exist.");
        var completionXp = DifficultyRules.GetCompletionXp(quest.Difficulty);

        var completion = await _context.QuestCompletions
            .FirstOrDefaultAsync(c => c.UserId == userId && c.QuestId == questId);
        var now = DateTime.UtcNow;
        var firstCompletion = completion is null;

        if (completion is null)
        {
            completion = new QuestCompletion { UserId = userId, QuestId = questId, FirstCompletedAt = now };
            _context.QuestCompletions.Add(completion);
            user.QuizzesCompleted++;
        }

        var pointsAwarded = firstCompletion ? completionXp : 0;
        completion.BestScore = Math.Max(completion.BestScore, score);
        completion.MaxScore = maxScore;
        completion.Attempts++;
        completion.LastCompletedAt = now;
        user.Points += pointsAwarded;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} completed quest {QuestId}: {Score}/{MaxScore}, +{PointsAwarded} XP (best {BestScore}, attempt {Attempt})",
            userId, questId, score, maxScore, pointsAwarded, completion.BestScore, completion.Attempts);

        return new QuestResult(score, maxScore, pointsAwarded, completion.BestScore, firstCompletion, user.Points);
    }
}
