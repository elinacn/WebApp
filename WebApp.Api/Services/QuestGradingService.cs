using System.Text.RegularExpressions;
using WebApp.Api.Dtos;
using WebApp.Api.Models;

namespace WebApp.Api.Services;

// Grades one attempt at a quest. Pure logic with no database access, so it is easy to test.
public partial class QuestGradingService
{
    public const string CodeBlank = "____";

    // Expects the quest's questions and options to be loaded
    public List<QuestionResultDto> Grade(Quest quest, IEnumerable<AnswerDto> answers)
    {
        var byQuestion = answers.GroupBy(a => a.QuestionId).ToDictionary(g => g.Key, g => g.First());

        return quest.Questions.OrderBy(q => q.Position).Select(question =>
        {
            byQuestion.TryGetValue(question.Id, out var answer);
            var accepted = DtoMapper.SplitLines(question.AcceptedAnswers);
            var correctKeys = DtoMapper.SplitKeys(question.CorrectOptionKeys);

            var isCorrect = question.Type switch
            {
                QuestionTypes.FreeText => IsFreeTextCorrect(accepted, answer?.Text, question.CaseSensitive),
                QuestionTypes.Code => IsCodeCorrect(accepted, answer?.Text),
                _ => IsChoiceCorrect(correctKeys, answer?.Selected)
            };

            return new QuestionResultDto(
                question.Id,
                isCorrect,
                question.Type == QuestionTypes.MultipleChoice ? correctKeys : [],
                question.Type == QuestionTypes.MultipleChoice ? [] : accepted,
                question.Explanation);
        }).ToList();
    }

    // Free text: must match one accepted answer exactly (ignoring surrounding spaces, and case unless case-sensitive)
    public static bool IsFreeTextCorrect(List<string> accepted, string? given, bool caseSensitive)
    {
        var text = (given ?? string.Empty).Trim();
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return text.Length > 0 && accepted.Any(a => string.Equals(a, text, comparison));
    }

    // Code: the fill-in for the ____ blank is compared as text (the code is never run),
    // ignoring whitespace, a trailing semicolon and a leading "return"
    public static bool IsCodeCorrect(List<string> accepted, string? given)
    {
        var code = NormalizeCode(given);
        return code.Length > 0 && accepted.Any(a => NormalizeCode(a) == code);
    }

    // Multiple choice: exactly the correct options must be selected, no more and no fewer
    public static bool IsChoiceCorrect(List<string> correctKeys, List<string>? selected)
    {
        var picked = (selected ?? []).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
        return picked.Count > 0 && picked.Count == correctKeys.Count && !picked.Except(correctKeys).Any();
    }

    // "return a + b;" -> "a+b". Stays case-sensitive, like real code.
    public static string NormalizeCode(string? code)
    {
        var text = LeadingReturn().Replace((code ?? string.Empty).Trim(), string.Empty);
        return Whitespace().Replace(text, string.Empty).TrimEnd(';');
    }

    [GeneratedRegex(@"^return\b")]
    private static partial Regex LeadingReturn();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
