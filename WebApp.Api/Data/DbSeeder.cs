using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using WebApp.Api.Models;

namespace WebApp.Api.Data;

// Fills an empty database with demo users, courses and the quizzes in Data/SeedData.
// Each part only runs when its table is empty, so data changed through the app is never overwritten.
public static class DbSeeder
{
    // Shared password for all demo users, so anyone on the team can sign in as any of them
    public const string DemoPassword = "quest1234";

    public static void Seed(AppDbContext db, string seedDataDir, ILogger logger)
    {
        SeedUsers(db, logger);
        SeedCourses(db, logger);
        SeedQuests(db, seedDataDir, logger);
    }

    private static void SeedUsers(AppDbContext db, ILogger logger)
    {
        if (db.Users.Any()) return;

        var hasher = new PasswordHasher<User>();
        var users = new List<User>
        {
            new() { Username = "admin", Name = "Admin", StudyProgram = "Teaching staff", Role = Roles.Admin },
            new() { Username = "anna", Name = "Anna", StudyProgram = "Web programming" },
            new() { Username = "beatrice", Name = "Beatrice", StudyProgram = "Web programming" },
            new() { Username = "calvin", Name = "Calvin", StudyProgram = "Programming basics" },
            new() { Username = "elina", Name = "Elina", StudyProgram = "Web programming" },
            new() { Username = "froed", Name = "Froed", StudyProgram = "Programming basics" },
            new() { Username = "johan", Name = "Johan", StudyProgram = "Web programming" },
            new() { Username = "oyvind", Name = "Øyvind", StudyProgram = "Programming basics" },
            new() { Username = "maya", Name = "Maya", StudyProgram = "Web programming" }
        };
        foreach (var user in users)
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);

        db.Users.AddRange(users);
        db.SaveChanges();
        logger.LogInformation("Seeded {Count} demo users", users.Count);
    }

    private static void SeedCourses(AppDbContext db, ILogger logger)
    {
        if (db.Courses.Any()) return;

        db.Courses.AddRange(
            new Course { Code = "PROG-101", Title = "Programming basics", Description = "Variables, types, operators and your first methods." },
            new Course { Code = "WEB-201", Title = "Web programming", Description = "Strings, logic and building blocks for web apps." });
        db.SaveChanges();
        logger.LogInformation("Seeded 2 courses");
    }

    private static void SeedQuests(AppDbContext db, string seedDataDir, ILogger logger)
    {
        if (db.Quests.Any()) return;

        if (!Directory.Exists(seedDataDir))
        {
            logger.LogWarning("Seed data folder {Dir} not found; no quests seeded", seedDataDir);
            return;
        }

        // Beginner quizzes go in the basics course, the rest in web programming
        var basics = db.Courses.FirstOrDefault(c => c.Code == "PROG-101");
        var web = db.Courses.FirstOrDefault(c => c.Code == "WEB-201");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        foreach (var file in Directory.EnumerateFiles(seedDataDir, "*.json").Order())
        {
            try
            {
                var quiz = JsonSerializer.Deserialize<QuizFile>(File.ReadAllText(file), options);
                var difficulty = DifficultyRules.Normalize(quiz?.Difficulty);
                if (quiz is null || string.IsNullOrWhiteSpace(quiz.Title) || difficulty is null)
                {
                    logger.LogWarning("Skipped {File}: not a valid quiz", Path.GetFileName(file));
                    continue;
                }

                var quest = new Quest
                {
                    Title = quiz.Title.Trim(),
                    Description = quiz.Description,
                    Difficulty = difficulty,
                    Course = difficulty == nameof(Difficulty.Beginner) ? basics : web
                };

                foreach (var source in quiz.Questions.Where(q => QuestionTypes.All.Contains(q.Type)))
                    quest.Questions.Add(ToQuestion(source, quest.Questions.Count));

                db.Quests.Add(quest);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Could not read quest file {File}", Path.GetFileName(file));
            }
        }

        db.SaveChanges();
        logger.LogInformation("Seeded {Count} quests from {Dir}", db.Quests.Count(), seedDataDir);
    }

    private static QuestQuestion ToQuestion(QuizQuestion source, int position)
    {
        var question = new QuestQuestion
        {
            Position = position,
            Type = source.Type,
            Text = source.Type == QuestionTypes.Code ? source.Text + FormatExamples(source) : source.Text,
            Points = source.Points,
            Explanation = source.Explaination,
            CaseSensitive = source.CaseSensitive,
            StartCode = source.StartCode,
            AcceptedAnswers = source.AcceptedAnswers.Count > 0 ? string.Join("\n", source.AcceptedAnswers) : null
        };

        if (source.Type == QuestionTypes.MultipleChoice)
        {
            question.Options = source.Options.Select(o => new QuestOption { Key = o.Id, Text = o.Text }).ToList();
            var correct = source.CorrectOptionIds.Count > 0 ? source.CorrectOptionIds : [source.CorrectOptionId ?? string.Empty];
            question.CorrectOptionKeys = string.Join(",", correct.Where(k => k.Length > 0));
        }

        return question;
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

    // Shape of the quiz JSON files ("explaination" is spelled as in the files)
    private sealed class QuizFile
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public List<QuizQuestion> Questions { get; set; } = [];
    }

    private sealed class QuizQuestion
    {
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
