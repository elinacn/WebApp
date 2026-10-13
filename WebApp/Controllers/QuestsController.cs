using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.ViewModels;

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
    private static readonly string[] QuestionTypes = ["multipleChoice", "freeText"];

    private readonly ILogger<QuestsController> _logger;
    private readonly SheetsDbContext _context;

    public QuestsController(ILogger<QuestsController> logger, SheetsDbContext context)
    {
        _logger = logger;
        _context = context;
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
            Difficulty = difficulty
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
            SavedQuests = LoadSavedQuests(filter)
        };

        if (section == "create" || section == "library")
        {
            model.MyQuests = await LoadMyQuestsAsync(section == "library" ? searchText : null);
        }

        return View(model);
    }

    // Save
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleSave(int questId)
    {
        _logger.LogInformation("Toggled the saved state for quest {QuestId}.", questId);

        // DATABASE HOOK: connect the database here, for saving quests type shi
        return RedirectToAction(nameof(Quests));
    }

    // Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "NewQuest")] CreateQuestViewModel newQuest,
        string? addQuestion,
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
        _context.Quests.Add(quest);
        await _context.SaveChangesAsync();

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
        if (quest is null) return NotFound();

        var newQuest = ToViewModel(quest);
        PrepareQuestions(newQuest);
        return await CreateFormAsync(newQuest, clearModelState: false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(Prefix = "NewQuest")] CreateQuestViewModel newQuest,
        string? addQuestion,
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
        if (quest is null) return NotFound();

        // Replace the old questions/options with what was posted
        _context.QuestQuestions.RemoveRange(quest.Questions);
        quest.Questions.Clear();
        ApplyToEntity(quest, newQuest);
        await _context.SaveChangesAsync();

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
        return quest is null ? NotFound() : View(quest);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var quest = await _context.Quests.FindAsync(id);
        if (quest is null) return RedirectToAction(nameof(Quests), new { section = "create" });

        _context.Quests.Remove(quest);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Quest deleted.";
        return RedirectToAction(nameof(Quests), new { section = "create" });
    }

    private async Task<IActionResult?> HandleQuestionButtonsAsync(
        CreateQuestViewModel newQuest, string? addQuestion, int? removeQuestion)
    {
        if (!string.IsNullOrEmpty(addQuestion))
        {
            newQuest.Questions.Add(new CreateQuestQuestionViewModel());
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

    private async Task<List<QuestListItemViewModel>> LoadMyQuestsAsync(string? searchText = null)
    {
        var query = _context.Quests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(q => EF.Functions.Like(q.Title, $"%{searchText}%"));
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
            foreach (var key in new[] { "a", "b", "c", "d" })
            {
                if (!question.Options.Any(o => o.Id == key))
                {
                    question.Options.Add(new CreateQuestOptionViewModel { Id = key });
                }
            }

            question.Options = question.Options.OrderBy(o => o.Id, StringComparer.Ordinal).ToList();
        }
    }

        private void ValidateQuest(CreateQuestViewModel newQuest)
    {
        for (var i = 0; i < newQuest.Questions.Count; i++)
        {
            var question = newQuest.Questions[i];
            var prefix = $"NewQuest.Questions[{i}]";

            if (!QuestionTypes.Contains(question.Type))
            {
                ModelState.AddModelError($"{prefix}.Type", "Choose a valid question type.");
                continue;
            }

            if (question.Type == "freeText")
            {
                if (SplitAnswers(question.AcceptedAnswers).Count == 0)
                    ModelState.AddModelError($"{prefix}.AcceptedAnswers", "Add at least one accepted answer (comma separated).");
                continue;
            }

            var filled = question.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList();
            if (filled.Count < 2)
            {
                ModelState.AddModelError($"{prefix}.Options", "Fill in at least two options.");
            }

            var correct = (question.CorrectOptionId ?? string.Empty).Trim();
            if (!filled.Any(o => string.Equals(o.Id, correct, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError($"{prefix}.CorrectOptionId", "The correct option must be one of the filled-in options.");
            }
        }
    }

    private static List<string> SplitAnswers(string? acceptedAnswers) =>
    (acceptedAnswers ?? string.Empty)
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .ToList();

    private static void ApplyToEntity(Quest quest, CreateQuestViewModel newQuest)
    {
        quest.Title = newQuest.Title.Trim();
        quest.Description = string.IsNullOrWhiteSpace(newQuest.Description) ? null : newQuest.Description.Trim();
        quest.Difficulty = newQuest.Difficulty.Trim();

        for (var i = 0; i < newQuest.Questions.Count; i++)
        {
            var source = newQuest.Questions[i];
            var question = new QuestQuestion
            {
                Position = i,
                Type = source.Type,
                Question = source.Question.Trim(),
                Points = source.Points,
                Explaination = string.IsNullOrWhiteSpace(source.Explaination) ? null : source.Explaination.Trim()
            };

            if (source.Type == "freeText")
            {
                question.AcceptedAnswers = string.Join(", ", SplitAnswers(source.AcceptedAnswers));
            }
            else
            {
                question.CorrectOptionId = (source.CorrectOptionId ?? string.Empty).Trim().ToLowerInvariant();
                foreach (var option in source.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)))
                {
                    question.Options.Add(new QuestOption
                    {
                        OptionKey = (option.Id ?? string.Empty).Trim().ToLowerInvariant(),
                        Text = option.Text!.Trim()
                    });
                }
            }

            quest.Questions.Add(question);
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
            .Select(q => new CreateQuestQuestionViewModel
            {
                Id = $"q{q.Position + 1}",
                Type = q.Type,
                Question = q.Question,
                Points = q.Points,
                CorrectOptionId = q.CorrectOptionId ?? "a",
                AcceptedAnswers = q.AcceptedAnswers,
                Explaination = q.Explaination,
                Options = q.Options
                    .Select(o => new CreateQuestOptionViewModel { Id = o.OptionKey, Text = o.Text })
                    .ToList()
            })
            .ToList()
    };
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
}
