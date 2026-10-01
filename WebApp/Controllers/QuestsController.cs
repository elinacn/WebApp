using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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
    Create creates a quest/quiz json file under tasks as of now this should probably be stored in the database (should've called it CreateQuest or something but can fix that later)



 */
public class QuestsController : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ILogger<QuestsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public QuestsController(ILogger<QuestsController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public IActionResult Index(string section = "library", string? searchText = null, Difficulty? difficulty = null)
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

        var model = new QuestsPageViewModel
        {
            ActiveSection = section,
            Filter = filter,
            ContinueQuests = LoadContinueQuests(filter),
            CompletedQuests = LoadCompletedQuests(filter),
            FeaturedQuests = LoadFeaturedQuests(filter),
            RecommendedQuests = LoadRecommendedQuests(filter),
            SavedQuests = LoadSavedQuests(filter)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(
        [Bind(Prefix = "NewQuest")] CreateQuestViewModel newQuest,
        string? addQuestion,
        int? removeQuestion)
    {
        PrepareQuestions(newQuest);

        if (!string.IsNullOrEmpty(addQuestion))
        {
            newQuest.Questions.Add(new CreateQuestQuestionViewModel());
            return CreateForm(newQuest);
        }

        if (removeQuestion is int index && index >= 0 && index < newQuest.Questions.Count && newQuest.Questions.Count > 1)
        {
            newQuest.Questions.RemoveAt(index);
            return CreateForm(newQuest);
        }

        var quizId = NextQuizId();
        var filePath = SaveQuizJson(quizId, newQuest);
        _logger.LogInformation("Saved quiz {QuizId} to {FilePath}.", quizId, filePath);

        // DATABASE HOOK: connect the database here if we're also gonna store quizzes in the database
        return RedirectToAction(nameof(Index), new { section = "library" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleSave(int questId)
    {
        _logger.LogInformation("Toggled the saved state for quest {QuestId}.", questId);

        // DATABASE HOOK: connect the database here, for saving quests type shi
        return RedirectToAction(nameof(Index));
    }

    private IActionResult CreateForm(CreateQuestViewModel newQuest)
    {
        ModelState.Clear();
        return View(nameof(Index), new QuestsPageViewModel
        {
            ActiveSection = "create",
            NewQuest = newQuest
        });
    }

    private static void PrepareQuestions(CreateQuestViewModel newQuest)
    {
        if (newQuest.Questions.Count == 0)
        {
            newQuest.Questions.Add(new CreateQuestQuestionViewModel());
        }

        foreach (var question in newQuest.Questions)
        {
            if (question.Options.Count == 0)
            {
                question.Options =
                [
                    new() { Id = "a" },
                    new() { Id = "b" },
                    new() { Id = "c" },
                    new() { Id = "d" }
                ];
            }
        }
    }

    private string NextQuizId()
    {
        var max = 0;
        var folder = Path.Combine(_environment.ContentRootPath, "Tasks");
        if (!Directory.Exists(folder))
        {
            return "quiz1";
        }

        foreach (var file in Directory.GetFiles(folder, "*.json"))
        {
            try
            {
                using var document = JsonDocument.Parse(System.IO.File.ReadAllText(file));
                if (!document.RootElement.TryGetProperty("id", out var idProperty))
                {
                    continue;
                }

                var id = idProperty.GetString() ?? string.Empty;
                if (id.StartsWith("quiz", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(id[4..], out var number))
                {
                    max = Math.Max(max, number);
                }
            }
            catch (JsonException)
            {
                // Skips files that are not quiz JSON.
            }
        }

        return $"quiz{max + 1}";
    }

    private string SaveQuizJson(string quizId, CreateQuestViewModel newQuest)
    {
        var folder = Path.Combine(_environment.ContentRootPath, "Tasks");
        Directory.CreateDirectory(folder);

        var questions = new List<Dictionary<string, object?>>();
        for (var i = 0; i < newQuest.Questions.Count; i++)
        {
            var question = newQuest.Questions[i];
            var item = new Dictionary<string, object?>
            {
                ["id"] = $"q{i + 1}",
                ["type"] = string.IsNullOrWhiteSpace(question.Type) ? "multipleChoice" : question.Type,
                ["text"] = question.Text,
                ["points"] = question.Points,
                ["explaination"] = question.Explaination
            };

            if (question.Type == "freeText")
            {
                item["acceptedAnswers"] = question.AcceptedAnswers
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                item["options"] = question.Options
                    .Select(option => new Dictionary<string, string>
                    {
                        ["id"] = option.Id,
                        ["text"] = option.Text
                    })
                    .ToList();
                item["correctOptionId"] = question.CorrectOptionId;
            }

            questions.Add(item);
        }

        var quiz = new Dictionary<string, object?>
        {
            ["questType"] = "quiz",
            ["id"] = quizId,
            ["title"] = newQuest.Title,
            ["description"] = newQuest.Description,
            ["difficulty"] = newQuest.Difficulty,
            ["questions"] = questions
        };

        var fileName = FileNameFromTitle(newQuest.Title, quizId) + ".json";
        var filePath = Path.Combine(folder, fileName);
        if (System.IO.File.Exists(filePath))
        {
            filePath = Path.Combine(folder, $"{FileNameFromTitle(newQuest.Title, quizId)}-{quizId}.json");
        }

        System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(quiz, JsonOptions));
        return filePath;
    }

    private static string FileNameFromTitle(string title, string fallback)
    {
        var slug = new string(title
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrEmpty(slug) ? fallback : slug;
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
}
