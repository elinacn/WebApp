using WebApp.Api.Models;

namespace WebApp.Api.Dtos;

// Converts entities to DTOs and form DTOs to entities, so controllers stay short
public static class DtoMapper
{
    public static readonly string[] OptionKeys = ["a", "b", "c", "d", "e", "f"];

    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Username, user.Name, user.StudyProgram, user.Role, user.Points, user.QuizzesCompleted, user.CreatedAt);

    public static CourseDto ToDto(this Course course) =>
        new(course.Id, course.Code, course.Title, course.Description, course.Quests.Count);

    // Expects Questions and Course to be loaded
    public static QuestSummaryDto ToSummaryDto(this Quest quest, int? bestPercent) =>
        new(quest.Id, quest.Title, quest.Description, quest.Difficulty, quest.CourseId, quest.Course?.Code,
            quest.Questions.Count, quest.Questions.Sum(q => q.Points),
            DifficultyRules.GetCompletionXp(quest.Difficulty), quest.CreatedAt, bestPercent);

    public static QuestPlayDto ToPlayDto(this Quest quest) =>
        new(quest.Id, quest.Title, quest.Description, quest.Difficulty, DifficultyRules.GetCompletionXp(quest.Difficulty),
            quest.Questions.OrderBy(q => q.Position).Select(q => new QuestionPlayDto(
                q.Id, q.Type, q.Text, q.Points,
                AllowMultiple: SplitKeys(q.CorrectOptionKeys).Count > 1,
                q.StartCode,
                q.Options.OrderBy(o => o.Key, StringComparer.Ordinal).Select(o => new OptionDto(o.Key, o.Text)).ToList()))
            .ToList());

    // Entity -> the edit form
    public static QuestFormDto ToFormDto(this Quest quest) => new()
    {
        Title = quest.Title,
        Description = quest.Description,
        Difficulty = quest.Difficulty,
        CourseId = quest.CourseId,
        Questions = quest.Questions.OrderBy(q => q.Position).Select(q =>
        {
            var correct = SplitKeys(q.CorrectOptionKeys);
            return new QuestionFormDto
            {
                Type = q.Type,
                Text = q.Text,
                Points = q.Points,
                CaseSensitive = q.CaseSensitive,
                StartCode = q.StartCode,
                Explanation = q.Explanation,
                AcceptedAnswers = SplitLines(q.AcceptedAnswers),
                Options = q.Options.OrderBy(o => o.Key, StringComparer.Ordinal)
                    .Select(o => new OptionFormDto { Text = o.Text, IsCorrect = correct.Contains(o.Key) })
                    .ToList()
            };
        }).ToList()
    };

    // Form -> entity. Replaces the quest's questions with the ones in the form.
    // Assumes the form passed QuestValidator, so only the fields of each question type are kept.
    public static void ApplyTo(this QuestFormDto form, Quest quest)
    {
        quest.Title = form.Title.Trim();
        quest.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        quest.Difficulty = DifficultyRules.Normalize(form.Difficulty) ?? nameof(Difficulty.Beginner);
        quest.CourseId = form.CourseId;
        quest.Questions.Clear();

        for (var i = 0; i < form.Questions.Count; i++)
        {
            var source = form.Questions[i];
            var question = new QuestQuestion
            {
                Position = i,
                Type = source.Type,
                Text = source.Text.Replace("\r\n", "\n").Trim(),
                Points = source.Points,
                Explanation = string.IsNullOrWhiteSpace(source.Explanation) ? null : source.Explanation.Trim()
            };

            if (source.Type == QuestionTypes.MultipleChoice)
            {
                // Empty options are dropped, the rest get the keys a, b, c ...
                var options = source.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList();
                question.Options = options.Select((o, n) => new QuestOption { Key = OptionKeys[n], Text = o.Text.Trim() }).ToList();
                question.CorrectOptionKeys = string.Join(",", options
                    .Select((o, n) => (o.IsCorrect, Key: OptionKeys[n]))
                    .Where(x => x.IsCorrect)
                    .Select(x => x.Key));
            }
            else
            {
                // Code is always compared case-sensitively; free text only when ticked
                question.CaseSensitive = source.Type == QuestionTypes.Code || source.CaseSensitive;
                question.AcceptedAnswers = string.Join("\n", CleanAnswers(source.AcceptedAnswers, question.CaseSensitive));
                if (source.Type == QuestionTypes.Code)
                    question.StartCode = (source.StartCode ?? string.Empty).Replace("\r\n", "\n").TrimEnd();
            }

            quest.Questions.Add(question);
        }
    }

    // Non-empty, trimmed and without duplicates ("Oslo" and "oslo" are the same unless case-sensitive)
    public static List<string> CleanAnswers(IEnumerable<string?> answers, bool caseSensitive = true) =>
        answers
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!.Trim())
            .Distinct(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static List<string> SplitLines(string? text) =>
        (text ?? string.Empty).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

    public static List<string> SplitKeys(string? keys) =>
        (keys ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
}
