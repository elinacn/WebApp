using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Extensions;
using WebApp.Models;
using WebApp.ViewModels;
using static WebApp.ViewModels.QuestFormMapper;
using WebApp.Services;

namespace WebApp.Controllers;

 /*
    For the database: 
    LoadContinueQuests returns the in-progress quests as the questcardviewmmodel items with difficulty for filters
    LoadCompletedQuests returns completed quests as QCVM items etc
    LoadFeatured loads featured quests with qcvm etc
    LoadRecommendedQuests (You can tell by the title)
    LoadSavedQuest should returns quests that the current user has saved
    toggleSave should save the quest or not (if its already saved)
    Create creates a quest json file under tasks as of now this should probably be stored in the database (should've called it CreateQuest or something but can fix that later)



 */
public class QuestsController : Controller
{
    private readonly ILogger<QuestsController> _logger;
    private readonly SheetsDbContext _context;
    private readonly DailyStreakService _streakService;
    private readonly LeaderboardDbContext _leaderboardContext;

    public QuestsController(
        ILogger<QuestsController> logger,
        SheetsDbContext context,
        DailyStreakService streakService,
        LeaderboardDbContext leaderboardContext) 
    {
        _logger = logger;
        _context = context;
        _streakService = streakService;
        _leaderboardContext = leaderboardContext; 
    private readonly QuestScoringService _scoringService;

    public QuestsController(
    ILogger<QuestsController> logger,
    SheetsDbContext context,
    DailyStreakService streakService,
    QuestScoringService scoringService)
    {
    _logger = logger;
    _context = context;
    _streakService = streakService;
    _scoringService = scoringService;
    }


    public async Task<IActionResult> Quests(string section = "library", string? searchText = null, Difficulty? difficulty = null)
    {
        _logger.LogInformation("Opened the Quests page for section {Section}.", section);

        if (section is not "library" and not "explore" and not "saved" and not "create")
        {
            section = "library";
        }

        var filter = new QuestFilterViewModel
        {
            SearchText = searchText,
        };

        var newQuest = new CreateQuestViewModel();
        PrepareQuestions(newQuest);

        var model = new QuestsPageViewModel
        {
            ActiveSection = section,
            Filter = filter,
            NewQuest = newQuest,
            ContinueQuests = LoadContinueQuests(filter),
            CompletedQuests = LoadCompletedQuests(filter),
            FeaturedQuests = LoadFeaturedQuests(filter),
            RecommendedQuests = LoadRecommendedQuests(filter),
            ExploreQuests = section == "explore" ? await LoadExploreQuestsAsync(filter) : [],
            SavedQuests = LoadSavedQuests(filter)
        };

        if (section == "create" || section == "library")
        {
            model.MyQuests = await LoadMyQuestsAsync(section == "library" ? filter : null);
        }

        return View(model);
    }

    // Save: only signed-in users can save quests; others are sent to the sign-in page
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleSave(int questId)
    {
        _logger.LogInformation("User {UserId} toggled the saved state for quest {QuestId}.", User.GetUserId(), questId);

        // DATABASE HOOK: connect the database here, for saving quests type shi
        return RedirectToAction(nameof(Quests));
    }

    // Play: signing in is required, so the points earned can be recorded for the user
    [Authorize]
    public async Task<IActionResult> Play(int? id)
    {
        if (id is null) return BadRequest();

        var quest = await _context.Quests
            .AsNoTracking()
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quest is null) return NotFound();

        if (quest.Questions.Count == 0)
        {
            TempData["Error"] = "This quest has no questions yet.";
            return RedirectToAction(nameof(Quests), new { section = "library" });
        }

        return View(ToPlayModel(quest));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Play(int id, List<PlayAnswerInput>? answers)
    {
        answers ??= [];

        var quest = await _context.Quests
            .AsNoTracking()
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quest is null) return NotFound();

        var model = ToPlayModel(quest);
        model.Submitted = true;

        foreach (var question in model.Questions)
        {
            var source = quest.Questions.First(q => q.Id == question.Id);
            var answer = answers.FirstOrDefault(a => a.QuestionId == question.Id);

            question.Explanation = source.Explaination;
            question.TextAnswer = answer?.Text;
            question.SelectedKeys = (answer?.Selected ?? [])
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct()
                .ToList();

            if (source.Type == "freeText")
            {
                var accepted = (source.AcceptedAnswers ?? string.Empty)
                    .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var comparison = source.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                var given = (answer?.Text ?? string.Empty).Trim();

                question.AcceptedAnswers = accepted.ToList();
                question.IsCorrect = given.Length > 0 && accepted.Any(a => string.Equals(a, given, comparison));
            }
            else if (source.Type == "code")
            {
                // Fill-in-the-blank: compared as text, ignoring whitespace (the code is never run)
                question.AcceptedAnswers = CodeAnswerChecker.SplitLines(source.AcceptedCode);
                question.IsCorrect = CodeAnswerChecker.IsCorrect(source, answer?.Text);
            }
            else
            {
                var correct = (source.CorrectOptionId ?? string.Empty)
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                question.CorrectKeys = correct;
                question.IsCorrect = question.SelectedKeys.Count > 0
                    && question.SelectedKeys.Count == correct.Count
                    && !question.SelectedKeys.Except(correct).Any();
            }
        }

        // [Authorize] guarantees a signed-in user with an ID
        var userId = User.GetUserId()!.Value;
        var score = model.Questions.Where(q => q.IsCorrect).Sum(q => q.Points);
        var maxScore = model.Questions.Sum(q => q.Points);

        try
        {
            model.Result = await _scoringService.RecordCompletionAsync(userId, quest.Id, score, maxScore);
        }
        catch (DbUpdateException ex)
        {
            // e.g. the same quest submitted twice at the same moment; the answers are still shown
            _logger.LogError(ex, "Failed to record completion of quest {QuestId} for user {UserId}", quest.Id, userId);
            model.ResultError = "Your answers were checked, but the points couldn't be saved. Please try again.";
        }

        await _streakService.RecordActivityAsync(userId.ToString());

        ModelState.Clear();
        return View(model);
    }

    private static PlayQuestViewModel ToPlayModel(Quest quest) => new()
    {
        Id = quest.Id,
        Title = quest.Title,
        Description = quest.Description,
        Difficulty = quest.Difficulty,
        Questions = quest.Questions
            .OrderBy(q => q.Position)
            .Select(q => new PlayQuestionViewModel
            {
                Id = q.Id,
                Type = q.Type,
                Question = q.Question,
                Points = q.Points,
                AllowMultiple = (q.CorrectOptionId ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).Length > 1,
                StartCode = q.StartCode,
                Options = q.Options
                    .OrderBy(o => o.OptionKey, StringComparer.Ordinal)
                    .Select(o => new PlayOptionViewModel { Key = o.OptionKey, Text = o.Text })
                    .ToList()
            })
            .ToList()
    };

    // Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "NewQuest")] CreateQuestViewModel newQuest,
        int? addQuestion,
        int? removeQuestion)
    {
        PrepareQuestions(newQuest);

        var buttonResult = await HandleQuestionButtonsAsync(newQuest, addQuestion, removeQuestion);
        if (buttonResult is not null) return buttonResult;

        newQuest.Id = 0;
        ValidateQuest(newQuest);
        var title = (newQuest.Title ?? string.Empty).Trim();
        if (await _context.Quests.AnyAsync(q => q.Title == title))
            ModelState.AddModelError("NewQuest.Title", "A quest with this title already exists.");

        if (!ModelState.IsValid) return await CreateFormAsync(newQuest, clearModelState: false);

        var quest = new Quest { CreatedAt = DateTime.Today };
        ApplyToEntity(quest, newQuest);

        try
        {
            _context.Quests.Add(quest);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created quest {QuestId} ({Title}) with {QuestionCount} questions", quest.Id, quest.Title, quest.Questions.Count);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create quest {Title}", quest.Title);
            ModelState.AddModelError(string.Empty, "Could not save the quest. Please try again.");
            return await CreateFormAsync(newQuest, clearModelState: false);
        }

        TempData["Success"] = "Quest created.";
        return RedirectToAction(nameof(Quests), new { section = "create" });
    }

    // Update
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return BadRequest();

        var quest = await _context.Quests
            .AsNoTracking()
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quest is null)
        {
            _logger.LogWarning("Quest {QuestId} not found for editing", id);
            return NotFound();
        }

        var newQuest = ToViewModel(quest);
        PrepareQuestions(newQuest);
        return await CreateFormAsync(newQuest, clearModelState: false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(Prefix = "NewQuest")] CreateQuestViewModel newQuest,
        int? addQuestion,
        int? removeQuestion)
    {
        if (id != newQuest.Id) return BadRequest();

        PrepareQuestions(newQuest);

        var buttonResult = await HandleQuestionButtonsAsync(newQuest, addQuestion, removeQuestion);
        if (buttonResult is not null) return buttonResult;

        ValidateQuest(newQuest);
        var title = (newQuest.Title ?? string.Empty).Trim();
        if (await _context.Quests.AnyAsync(q => q.Title == title && q.Id != id))
            ModelState.AddModelError("NewQuest.Title", "A quest with this title already exists.");

        if (!ModelState.IsValid) return await CreateFormAsync(newQuest, clearModelState: false);

        var quest = await _context.Quests
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quest is null)
        {
            _logger.LogWarning("Quest {QuestId} not found for saving", id);
            return NotFound();
        }

        // Replace the old questions/options with what was posted
        _context.QuestQuestions.RemoveRange(quest.Questions);
        quest.Questions.Clear();
        ApplyToEntity(quest, newQuest);

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated quest {QuestId} ({Title})", id, quest.Title);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Someone deleted the quest while this edit was open
            if (!await _context.Quests.AnyAsync(q => q.Id == id)) return NotFound();
            _logger.LogError(ex, "Concurrency conflict updating quest {QuestId}", id);
            ModelState.AddModelError(string.Empty, "This quest was changed elsewhere. Reload and try again.");
            return await CreateFormAsync(newQuest, clearModelState: false);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update quest {QuestId}", id);
            ModelState.AddModelError(string.Empty, "Could not save the quest. Please try again.");
            return await CreateFormAsync(newQuest, clearModelState: false);
        }

        TempData["Success"] = "Quest updated.";
        return RedirectToAction(nameof(Quests), new { section = "create" });
    }

    // Delete
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return BadRequest();
        var quest = await _context.Quests
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quest is null)
        {
            _logger.LogWarning("Quest {QuestId} not found for deletion", id);
            return NotFound();
        }
        return View(quest);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var quest = await _context.Quests.FindAsync(id);
        if (quest is null) return RedirectToAction(nameof(Quests), new { section = "create" });

        try
        {
            // Its questions and options are removed by the database (cascade delete)
            _context.Quests.Remove(quest);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted quest {QuestId} ({Title})", id, quest.Title);
            TempData["Success"] = "Quest deleted.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to delete quest {QuestId}", id);
            TempData["Error"] = "Could not delete the quest.";
        }
        return RedirectToAction(nameof(Quests), new { section = "create" });
    }

    private async Task<IActionResult?> HandleQuestionButtonsAsync(
        CreateQuestViewModel newQuest, int? addQuestion, int? removeQuestion)
    {
        // addQuestion = index of the question whose "+" was clicked, the new one goes right after it
        if (addQuestion is int after)
        {
            var position = Math.Clamp(after + 1, 0, newQuest.Questions.Count);
            newQuest.Questions.Insert(position, new CreateQuestQuestionViewModel());
            return await CreateFormAsync(newQuest);
        }

        if (removeQuestion is int index && index >= 0 && index < newQuest.Questions.Count && newQuest.Questions.Count > 1)
        {
            newQuest.Questions.RemoveAt(index);
            return await CreateFormAsync(newQuest);
        }

        return null;
    }

    private async Task<IActionResult> CreateFormAsync(CreateQuestViewModel newQuest, bool clearModelState = true)
    {
        if (clearModelState) ModelState.Clear();
        return View(nameof(Quests), new QuestsPageViewModel
        {
            ActiveSection = "create",
            NewQuest = newQuest,
            MyQuests = await LoadMyQuestsAsync()
        });
    }

    private async Task<List<QuestListItemViewModel>> LoadMyQuestsAsync(QuestFilterViewModel? filter = null)
    {
        var query = _context.Quests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter?.SearchText))
        {
            var text = filter.SearchText;
            query = query.Where(q => EF.Functions.Like(q.Title, $"%{text}%"));
        }

        if (filter?.Difficulty is Difficulty level)
        {
            var levelName = level.ToString();
            query = query.Where(q => EF.Functions.Like(q.Difficulty, levelName));
        }

        return await query
        .OrderBy(q => q.Title)
        .Select(q => new QuestListItemViewModel
        {
            Id = q.Id,
            Title = q.Title,
            Difficulty = q.Difficulty,
            CreatedAt = q.CreatedAt,
            QuestionCount = q.Questions.Count,
            TotalPoints = q.Questions.Sum(x => (int?)x.Points) ?? 0
        })
        .ToListAsync();
    }
    

    private static void PrepareQuestions(CreateQuestViewModel newQuest)
    {
        if (newQuest.Questions.Count == 0)
        {
            newQuest.Questions.Add(new CreateQuestQuestionViewModel());
        }

        foreach (var question in newQuest.Questions)
        {
            if (question.Options.Count > CreateQuestQuestionViewModel.MaxOptions)
                question.Options = question.Options.Take(CreateQuestQuestionViewModel.MaxOptions).ToList();

            while (question.Options.Count < CreateQuestQuestionViewModel.MinOptions)
                question.Options.Add(new CreateQuestOptionViewModel());

            // Keys always follow in alphabetical order: a, b, c, d, e, f
            for (var o = 0; o < question.Options.Count; o++)
                question.Options[o].Id = OptionKeys[o];

            var validIds = question.Options.Select(o => o.Id).ToList();
            question.CorrectOptionIds = question.CorrectOptionIds
                .Where(id => validIds.Contains(id))
                .Distinct()
                .ToList();

            if (question.AcceptedAnswers.Count == 0)
                question.AcceptedAnswers.Add(string.Empty);
        }
    }

    private void ValidateQuest(CreateQuestViewModel newQuest)
    {
        var chosen = (newQuest.Difficulty ?? string.Empty).Trim();
        if (!Enum.GetNames<Difficulty>().Contains(chosen, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("NewQuest.Difficulty", "Choose a difficulty.");
        }

        for (var i = 0; i < newQuest.Questions.Count; i++)
        {
            var question = newQuest.Questions[i];
            var prefix = $"NewQuest.Questions[{i}]";

            if (!QuestionTypes.Contains(question.Type))
            {
                ModelState.AddModelError($"{prefix}.Type", "Choose a valid question type.");
                continue;
            }

            if (question.Type == "code")
            {
                var blanks = (question.StartCode ?? string.Empty).Split(CodeAnswerChecker.Blank).Length - 1;
                if (blanks != 1)
                    ModelState.AddModelError($"{prefix}.StartCode", $"The start code must contain exactly one {CodeAnswerChecker.Blank} blank.");
                if (CodeAnswerChecker.SplitLines(question.AcceptedCode).Count == 0)
                    ModelState.AddModelError($"{prefix}.AcceptedCode", "Add at least one accepted answer (one per line).");
                continue;
            }

            if (question.Type == "freeText")
            {
                var answers = CleanAnswers(question.AcceptedAnswers, question.CaseSensitive);
                if (answers.Count == 0)
                    ModelState.AddModelError($"{prefix}.AcceptedAnswers", "Add at least one correct answer.");
                else if (string.Join("\n", answers).Length > 500)
                    ModelState.AddModelError($"{prefix}.AcceptedAnswers", "The correct answers can be at most 500 characters in total.");
                continue;
            }

            var filled = question.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList();
            if (filled.Count < 2)
                ModelState.AddModelError($"{prefix}.Options", "Fill in at least two options.");

            if (question.CorrectOptionIds.Count == 0)
                ModelState.AddModelError($"{prefix}.CorrectOptionIds", "Tick at least one correct option.");
            else if (question.CorrectOptionIds.Any(id => !filled.Any(o => o.Id == id)))
                ModelState.AddModelError($"{prefix}.CorrectOptionIds", "A correct option can't be empty.");
        }

    }

    private static CreateQuestViewModel ToViewModel(Quest quest) => new()
    {
        Id = quest.Id,
        QuestType = "quest",
        Title = quest.Title,
        Description = quest.Description,
        Difficulty = quest.Difficulty,
        Questions = quest.Questions
            .OrderBy(q => q.Position)
            .Select(q =>
            {
                var options = q.Options.OrderBy(o => o.OptionKey, StringComparer.Ordinal).ToList();
                var correctKeys = (q.CorrectOptionId ?? string.Empty)
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                return new CreateQuestQuestionViewModel
                {
                    Id = $"q{q.Position + 1}",
                    Type = q.Type,
                    Question = q.Question,
                    Points = q.Points,
                    Explaination = q.Explaination,
                    CaseSensitive = q.CaseSensitive,
                    AcceptedAnswers = (q.AcceptedAnswers ?? string.Empty)
                        .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => (string?)a)
                        .ToList(),
                    Options = options
                        .Select(o => new CreateQuestOptionViewModel { Text = o.Text })
                        .ToList(),
                    CorrectOptionIds = options
                        .Select((o, index) => (o.OptionKey, index))
                        .Where(x => correctKeys.Contains(x.OptionKey))
                        .Select(x => OptionKeys[x.index])
                        .ToList()
                };
            })
            .ToList()
    };

        private async Task<List<QuestCardViewModel>> LoadExploreQuestsAsync(QuestFilterViewModel filter)
    {
        var query = _context.Quests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            query = query.Where(q => EF.Functions.Like(q.Title, $"%{filter.SearchText}%"));
        }

        if (filter.Difficulty is Difficulty level)
        {
            var levelName = level.ToString();
            query = query.Where(q => EF.Functions.Like(q.Difficulty, levelName));
        }

        var cards = await query
            .OrderByDescending(q => q.CreatedAt)
            .ThenBy(q => q.Title)
            .Select(q => new QuestCardViewModel
            {
                Id = q.Id,
                Title = q.Title,
                DifficultyLabel = q.Difficulty,
                Points = q.Questions.Sum(x => (int?)x.Points) ?? 0,
                Status = QuestStatus.NotStarted
            })
            .ToListAsync();

        // Mark the quests the signed-in user has completed, with their best score as the progress
        if (User.GetUserId() is int userId)
        {
            var bestScores = await _context.QuestCompletions.AsNoTracking()
                .Where(c => c.UserId == userId)
                .ToDictionaryAsync(c => c.QuestId, c => c.MaxScore == 0 ? 100 : c.BestScore * 100 / c.MaxScore);

            foreach (var card in cards)
            {
                if (bestScores.TryGetValue(card.Id, out var percent))
                {
                    card.Status = QuestStatus.Completed;
                    card.ProgressPercent = percent;
                }
            }
        }

        return cards;
    }

    private List<QuestCardViewModel> LoadContinueQuests(QuestFilterViewModel filter)
    {
        // DATABASE HOOK: connect the database here. 
        return [];
    }

    private List<QuestCardViewModel> LoadCompletedQuests(QuestFilterViewModel filter)
    {
        // DATABASE HOOK: connect the database here. 
        return [];
    }

    private List<QuestCardViewModel> LoadFeaturedQuests(QuestFilterViewModel filter)
    {
        // DATABASE HOOK: connect the database here. 
        return [];
    }

    private List<QuestCardViewModel> LoadRecommendedQuests(QuestFilterViewModel filter)
    {
        // DATABASE HOOK: connect the database here. 
        return [];
    }

    private List<QuestCardViewModel> LoadSavedQuests(QuestFilterViewModel filter)
    {
        // DATABASE HOOK: connect the database here. 
        return [];
    }

    // testing for leaderboard functionality, simulates a quest completion and logs the score to the leaderboard
    [HttpGet]
    public async Task<IActionResult> SimulateQuest(int id = 1)
    {
        var quest = await _context.Quests
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == id);
    
        if (quest is null) return Content("Quest not found. Create a quest first.");
    
        var attempt = new QuestAttempt
        {
            StudentName = User.Identity?.Name ?? "TestUser",
            QuestId = quest.Id,
            PointsEarned = quest.Questions.Sum(q => q.Points),
            CompletedAt = DateTime.UtcNow
        };
    
        _leaderboardContext.QuestAttempts.Add(attempt);
        await _leaderboardContext.SaveChangesAsync();
    
        return RedirectToAction("Leaderboard", "Leaderboard");
    }
}
