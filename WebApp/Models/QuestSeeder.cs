using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;

namespace WebApp.Models;

// Pre-populated quests from the JSON quizzes in the Tasks folder. Only runs when the
// Quests table is empty, so quests created, edited or deleted on the site are left alone.
// Each file is read into the same form model the quest form posts, and saved with
// QuestFormMapper, so seeded quests are stored exactly like quests created on the site.
// Code questions are fill-in-the-blank: their test cases are shown as examples, not run.
public static class QuestSeeder
{
    public static void Seed(SheetsDbContext context, string tasksDir, ILogger logger)
    {
        if (context.Quests.Any()) return;

        if (!Directory.Exists(tasksDir))
        {
            logger.LogWarning("Tasks folder {TasksDir} not found; no quests seeded", tasksDir);
            return;
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var file in Directory.EnumerateFiles(tasksDir, "*.json").Order())
        {
            var name = Path.GetFileName(file);
            try
            {
                var quiz = JsonSerializer.Deserialize<QuizFile>(File.ReadAllText(file), options);
                if (quiz is null || quiz.QuestType != "quiz" || string.IsNullOrWhiteSpace(quiz.Title))
                {
                    logger.LogWarning("Skipped {File}: not a quiz", name);
                    continue;
                }

                // Quest titles are unique in the database
                if (!titles.Add(quiz.Title.Trim()))
                {
                    logger.LogWarning("Skipped {File}: duplicate title {Title}", name, quiz.Title);
                    continue;
                }

                var form = ToForm(quiz, name, logger);
                if (form.Questions.Count == 0)
                {
                    logger.LogWarning("Skipped {File}: no usable questions", name);
                    continue;
                }

                var quest = new Quest { CreatedAt = DateTime.Today };
                QuestFormMapper.ApplyToEntity(quest, form);
                context.Quests.Add(quest);
                added++;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Could not read quest file {File}", name);
            }
        }

        try
        {
            context.SaveChanges();
            logger.LogInformation("Seeded {Count} quests from {TasksDir}", added, tasksDir);
        }
        catch (DbUpdateException ex)
        {
            // Don't stop the app from starting because of bad seed data
            logger.LogError(ex, "Failed to save seeded quests");
            context.ChangeTracker.Clear();
        }
    }

    // JSON file -> the same model the quest form posts
    private static CreateQuestViewModel ToForm(QuizFile quiz, string file, ILogger logger)
    {
        var form = new CreateQuestViewModel
        {
            Title = quiz.Title,
            Description = quiz.Description,
            Difficulty = quiz.Difficulty,
            Questions = []
        };

        foreach (var source in quiz.Questions)
        {
            if (!QuestFormMapper.QuestionTypes.Contains(source.Type))
            {
                logger.LogWarning("Skipped question {QuestionId} in {File}: unknown type {Type}", source.Id, file, source.Type);
                continue;
            }

            if (source.Options.Count > CreateQuestQuestionViewModel.MaxOptions)
            {
                logger.LogWarning("Skipped question {QuestionId} in {File}: more than {Max} options", source.Id, file, CreateQuestQuestionViewModel.MaxOptions);
                continue;
            }

            form.Questions.Add(new CreateQuestQuestionViewModel
            {
                Type = source.Type,
                Question = source.Type == "code" ? source.Text + FormatExamples(source) : source.Text,
                Points = source.Points,
                Explaination = source.Explaination,
                Options = source.Options.Select(o => new CreateQuestOptionViewModel { Id = o.Id, Text = o.Text }).ToList(),
                CorrectOptionIds = [.. source.CorrectOptionIds, .. source.CorrectOptionId is null ? [] : new[] { source.CorrectOptionId }],
                CaseSensitive = source.CaseSensitive,
                AcceptedAnswers = [.. source.AcceptedAnswers],
                StartCode = source.StartCode,
                AcceptedCode = string.Join("\n", source.AcceptedAnswers)
            });
        }

        return form;
    }

    // Test cases become examples under the question text, e.g. "sum(1, 5) → 6"
    private static string FormatExamples(QuizQuestion source)
    {
        var method = Regex.Match(source.StartCode ?? string.Empty, @"static\s+\S+\s+(\w+)\s*\(");
        if (!method.Success || source.Tests.Count == 0) return string.Empty;

        var lines = source.Tests.Select(t =>
            $"{method.Groups[1].Value}({string.Join(", ", t.Input.Select(i => i.GetRawText()))}) → {t.Expected.GetRawText()}");
        return "\n\nExamples:\n" + string.Join("\n", lines);
    }

    // Shape of the quiz JSON files. "explaination" is spelled as in the files.
    // A multiple choice question can give one correctOptionId or several correctOptionIds.
    private sealed class QuizFile
    {
        public string QuestType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public List<QuizQuestion> Questions { get; set; } = [];
    }

    private sealed class QuizQuestion
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Points { get; set; } = 1;
        public List<QuizOption> Options { get; set; } = [];
        public string? CorrectOptionId { get; set; }
        public List<string> CorrectOptionIds { get; set; } = [];
        public bool CaseSensitive { get; set; }
        public List<string> AcceptedAnswers { get; set; } = [];
        public string? StartCode { get; set; }
        public List<QuizTest> Tests { get; set; } = [];
        public string? Explaination { get; set; }
    }

    // Inputs and expected values can be numbers, booleans or strings, so they stay raw JSON
    private sealed class QuizTest
    {
        public List<JsonElement> Input { get; set; } = [];
        public JsonElement Expected { get; set; }
    }

    private sealed class QuizOption
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
