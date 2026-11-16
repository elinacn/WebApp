using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Data;
using WebApp.Api.Dtos;
using WebApp.Api.Extensions;
using WebApp.Api.Models;
using WebApp.Api.Services;

namespace WebApp.Api.Controllers;

[ApiController]
[Route("api/quests")]
public class QuestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly QuestGradingService _grading;
    private readonly QuestScoringService _scoring;
    private readonly StreakService _streaks;
    private readonly ILogger<QuestsController> _logger;

    public QuestsController(
        AppDbContext db,
        QuestGradingService grading,
        QuestScoringService scoring,
        StreakService streaks,
        ILogger<QuestsController> logger)
    {
        _db = db;
        _grading = grading;
        _scoring = scoring;
        _streaks = streaks;
        _logger = logger;
    }

    // GET /api/quests?search=loop&difficulty=Beginner&courseId=2&status=notCompleted&sort=xp
    // Every filter is optional. status (completed / notCompleted) only applies when signed in.
    [HttpGet]
    public async Task<ActionResult<List<QuestSummaryDto>>> Search(
        string? search, string? difficulty, int? courseId, string? status, string? sort)
    {
        var userId = User.GetUserId();
        var query = _db.Quests.AsNoTracking().Include(q => q.Questions).Include(q => q.Course).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(q => EF.Functions.Like(q.Title, $"%{text}%")
                || (q.Description != null && EF.Functions.Like(q.Description, $"%{text}%")));
        }

        if (DifficultyRules.Normalize(difficulty) is string level)
            query = query.Where(q => q.Difficulty == level);

        if (courseId is not null)
            query = query.Where(q => q.CourseId == courseId);

        if (userId is not null && status is "completed" or "notCompleted")
        {
            var completed = _db.QuestCompletions.Where(c => c.UserId == userId).Select(c => c.QuestId);
            query = status == "completed"
                ? query.Where(q => completed.Contains(q.Id))
                : query.Where(q => !completed.Contains(q.Id));
        }

        var quests = await query.ToListAsync();
        var bestPercent = userId is null ? [] : await GetBestPercentAsync(userId.Value);

        // Sorted in memory: difficulty and XP are not stored as numbers
        IEnumerable<Quest> sorted = sort switch
        {
            "title" => quests.OrderBy(q => q.Title),
            "difficulty" => quests.OrderBy(q => DifficultyRules.GetCompletionXp(q.Difficulty)).ThenBy(q => q.Title),
            "xp" => quests.OrderByDescending(q => DifficultyRules.GetCompletionXp(q.Difficulty)).ThenBy(q => q.Title),
            _ => quests.OrderByDescending(q => q.CreatedAt).ThenBy(q => q.Title)
        };

        return sorted.Select(q => q.ToSummaryDto(bestPercent.TryGetValue(q.Id, out var p) ? p : null)).ToList();
    }

    // GET /api/quests/5/play – the questions without the answers
    [Authorize]
    [HttpGet("{id:int}/play")]
    public async Task<ActionResult<QuestPlayDto>> GetForPlay(int id)
    {
        var quest = await LoadQuestAsync(id);
        if (quest is null) return NotFound();

        if (quest.Questions.Count == 0)
            return Conflict(new ProblemDetails { Status = 409, Title = "This quest has no questions yet." });

        return quest.ToPlayDto();
    }

    // POST /api/quests/5/submit – grades the answers, awards XP and records today's streak
    [Authorize]
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<QuizResultDto>> Submit(int id, SubmitAnswersDto submission)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var quest = await LoadQuestAsync(id);
        if (quest is null) return NotFound();

        var userId = User.GetUserId()!.Value;
        var results = _grading.Grade(quest, submission.Answers ?? []);
        var points = quest.Questions.ToDictionary(q => q.Id, q => q.Points);
        var score = results.Where(r => r.IsCorrect).Sum(r => points[r.QuestionId]);
        var maxScore = points.Values.Sum();

        try
        {
            var completion = await _scoring.RecordCompletionAsync(userId, quest, score, maxScore);
            await _streaks.RecordActivityAsync(userId);

            return new QuizResultDto(score, maxScore, completion.XpAwarded, completion.FirstCompletion,
                completion.BestScore, completion.TotalXp, Saved: true, results);
        }
        catch (DbUpdateException ex)
        {
            // e.g. the same quest submitted twice at the same moment. The graded answers are
            // still returned, so the student sees their result, with a note that it wasn't saved.
            _logger.LogError(ex, "Failed to save the result of quest {QuestId} for user {UserId}", id, userId);
            return new QuizResultDto(score, maxScore, 0, false, score, 0, Saved: false, results);
        }
    }

    // GET /api/quests/5 – the quest as an edit form, answers included (admins only)
    [Authorize(Roles = Roles.Admin)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestFormDto>> GetForEdit(int id)
    {
        var quest = await LoadQuestAsync(id);
        return quest is null ? NotFound() : quest.ToFormDto();
    }

    // POST /api/quests
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<ActionResult<QuestSummaryDto>> Create(QuestFormDto form)
    {
        await ValidateAsync(form, questId: 0);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var quest = new Quest { CreatedAt = DateTime.UtcNow };
        form.ApplyTo(quest);
        _db.Quests.Add(quest);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created quest {QuestId} ({Title}) with {Count} questions", quest.Id, quest.Title, quest.Questions.Count);
        await _db.Entry(quest).Reference(q => q.Course).LoadAsync();
        return CreatedAtAction(nameof(GetForEdit), new { id = quest.Id }, quest.ToSummaryDto(null));
    }

    // PUT /api/quests/5 – replaces the quest and all its questions
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, QuestFormDto form)
    {
        var quest = await _db.Quests.Include(q => q.Questions).ThenInclude(q => q.Options).FirstOrDefaultAsync(q => q.Id == id);
        if (quest is null) return NotFound();

        await ValidateAsync(form, id);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        _db.QuestQuestions.RemoveRange(quest.Questions);
        form.ApplyTo(quest);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated quest {QuestId} ({Title})", id, quest.Title);
        return NoContent();
    }

    // DELETE /api/quests/5 – its questions and completions are deleted by the database (cascade)
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var quest = await _db.Quests.FindAsync(id);
        if (quest is null) return NotFound();

        _db.Quests.Remove(quest);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted quest {QuestId} ({Title})", id, quest.Title);
        return NoContent();
    }

    private Task<Quest?> LoadQuestAsync(int id) =>
        _db.Quests.AsNoTracking()
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

    // The signed-in user's best score per completed quest, as a percentage
    private Task<Dictionary<int, int>> GetBestPercentAsync(int userId) =>
        _db.QuestCompletions.AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToDictionaryAsync(c => c.QuestId, c => c.MaxScore == 0 ? 100 : c.BestScore * 100 / c.MaxScore);

    // Domain rules (QuestValidator) plus the checks that need the database
    private async Task ValidateAsync(QuestFormDto form, int questId)
    {
        foreach (var (key, message) in QuestValidator.Validate(form))
            ModelState.AddModelError(key, message);

        var title = form.Title.Trim();
        if (await _db.Quests.AnyAsync(q => q.Title == title && q.Id != questId))
            ModelState.AddModelError(nameof(QuestFormDto.Title), "A quest with this title already exists.");

        if (form.CourseId is int courseId && !await _db.Courses.AnyAsync(c => c.Id == courseId))
            ModelState.AddModelError(nameof(QuestFormDto.CourseId), "The selected course doesn't exist.");

        if (!ModelState.IsValid)
            _logger.LogInformation("Rejected quest form {Title}: {Count} validation errors", title, ModelState.ErrorCount);
    }
}
