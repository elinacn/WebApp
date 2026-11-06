using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Services;

// The outcome of one completed attempt, shown on the Play page after submitting
public record QuestResult(int Score, int MaxScore, int PointsAwarded, int BestScore, bool FirstCompletion, int TotalPoints);

// Awards code points for completed quests. The best score counts: a retake only
// earns the amount by which it beats the user's previous best on that quest, so
// practising never costs points and repeating an easy quest can't farm them.
// User.Points and User.QuizzesCompleted are updated in the same save, so the
// leaderboard and the Users pages stay in step with the completions.
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

        var pointsAwarded = Math.Max(0, score - completion.BestScore);
        completion.BestScore = Math.Max(completion.BestScore, score);
        completion.MaxScore = maxScore;
        completion.Attempts++;
        completion.LastCompletedAt = now;
        user.Points += pointsAwarded;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} completed quest {QuestId}: {Score}/{MaxScore}, +{PointsAwarded} code points (best {BestScore}, attempt {Attempt})",
            userId, questId, score, maxScore, pointsAwarded, completion.BestScore, completion.Attempts);

        return new QuestResult(score, maxScore, pointsAwarded, completion.BestScore, firstCompletion, user.Points);
    }
}
