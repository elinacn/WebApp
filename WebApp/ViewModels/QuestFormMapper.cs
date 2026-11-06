using WebApp.Models;

namespace WebApp.ViewModels;

// Turns the quest form (CreateQuestViewModel) into database entities. Shared by
// QuestsController and QuestSeeder, so seeded quests are stored exactly like
// quests created on the site.
public static class QuestFormMapper
{
    public static readonly string[] QuestionTypes = ["multipleChoice", "freeText", "code"];

    public static readonly string[] OptionKeys = ["a", "b", "c", "d", "e", "f"];

    // Removes blank boxes and duplicates
    public static List<string> CleanAnswers(IEnumerable<string?> answers, bool caseSensitive) =>
        answers
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!.Trim())
            .Distinct(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static void ApplyToEntity(Quest quest, CreateQuestViewModel newQuest)
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
                Question = source.Question.Replace("\r\n", "\n").Trim(),
                Points = source.Points,
                Explaination = string.IsNullOrWhiteSpace(source.Explaination) ? null : source.Explaination.Trim()
            };

            if (source.Type == "code")
            {
                question.StartCode = (source.StartCode ?? string.Empty).Replace("\r\n", "\n").TrimEnd();
                question.AcceptedCode = string.Join("\n", CodeAnswerChecker.SplitLines(source.AcceptedCode));
            }
            else if (source.Type == "freeText")
            {
                question.CaseSensitive = source.CaseSensitive;
                question.AcceptedAnswers = string.Join("\n", CleanAnswers(source.AcceptedAnswers, source.CaseSensitive));
            }
            else
            {
                // Empty options are skipped, the rest are renumbered a, b, c ...
                var correctKeys = new List<string>();
                var next = 0;
                foreach (var option in source.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)))
                {
                    var key = OptionKeys[next++];
                    question.Options.Add(new QuestOption { OptionKey = key, Text = option.Text!.Trim() });
                    if (source.CorrectOptionIds.Contains(option.Id)) correctKeys.Add(key);
                }
                question.CorrectOptionId = string.Join(",", correctKeys);
            }

            quest.Questions.Add(question);
        }
    }
}
