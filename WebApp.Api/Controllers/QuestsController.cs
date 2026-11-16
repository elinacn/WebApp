using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.DAL;
using WebApp.Api.Dtos;
using WebApp.Api.Extensions;
using WebApp.Api.Models;
using WebApp.Api.Services;

namespace WebApp.Api.Controllers;

[ApiController]
[Route("api/quests")]
public class QuestsController : ControllerBase
{
    private readonly IQuestRepository _quests;
    private readonly QuestGradingService _grading;
    private readonly QuestScoringService _scoring;
    private readonly StreakService _streaks;
    private readonly ILogger<QuestsController> _logger;

    public QuestsController(
        IQuestRepository quests,
        QuestGradingService grading,
        QuestScoringService scoring,
        StreakService streaks,
        ILogger<QuestsController> logger)
    {
        _quests = quests;
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
        var quests = await _quests.SearchAsync(search, difficulty, courseId, status, userId);
        var bestPercent = userId is null ? [] : await _quests.GetBestPercentAsync(userId.Value);

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
        var quest = await _quests.GetWithQuestionsAsync(id);
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

        var quest = await _quests.GetWithQuestionsAsync(id);
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
        var quest = await _quests.GetWithQuestionsAsync(id);
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
        await _quests.AddAsync(quest);

        _logger.LogInformation("Created quest {QuestId} ({Title}) with {Count} questions", quest.Id, quest.Title, quest.Questions.Count);
        return CreatedAtAction(nameof(GetForEdit), new { id = quest.Id }, quest.ToSummaryDto(null));
    }

    // PUT /api/quests/5 – replaces the quest and all its questions
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, QuestFormDto form)
    {
        var quest = await _quests.GetWithQuestionsAsync(id, tracking: true);
        if (quest is null) return NotFound();

        await ValidateAsync(form, id);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        form.ApplyTo(quest);
        await _quests.UpdateAsync(quest);

        _logger.LogInformation("Updated quest {QuestId} ({Title})", id, quest.Title);
        return NoContent();
    }

    // DELETE /api/quests/5 – its questions and completions are deleted by the database (cascade)
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var quest = await _quests.GetByIdAsync(id);
        if (quest is null) return NotFound();

        await _quests.DeleteAsync(quest);

        _logger.LogInformation("Deleted quest {QuestId} ({Title})", id, quest.Title);
        return NoContent();
    }

    // Domain rules (QuestValidator) plus the checks that need the database
    private async Task ValidateAsync(QuestFormDto form, int questId)
    {
        foreach (var (key, message) in QuestValidator.Validate(form))
            ModelState.AddModelError(key, message);

        var title = form.Title.Trim();
        if (await _quests.TitleExistsAsync(title, questId))
            ModelState.AddModelError(nameof(QuestFormDto.Title), "A quest with this title already exists.");

        if (form.CourseId is int courseId && !await _quests.CourseExistsAsync(courseId))
            ModelState.AddModelError(nameof(QuestFormDto.CourseId), "The selected course doesn't exist.");

        if (!ModelState.IsValid)
            _logger.LogInformation("Rejected quest form {Title}: {Count} validation errors", title, ModelState.ErrorCount);
    }
}
