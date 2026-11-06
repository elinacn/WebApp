using System.Text.RegularExpressions;

namespace WebApp.Models;

// Grades fill-in-the-blank code questions by comparing text, not by running the code.
// The student's fill-in counts as correct if it matches one of the accepted answers
// once whitespace, a trailing semicolon and a leading "return" are ignored.
public static partial class CodeAnswerChecker
{
    public const string Blank = "____";

    public static bool IsCorrect(QuestQuestion question, string? answer)
    {
        var given = Normalize(answer);
        return given.Length > 0 && SplitLines(question.AcceptedCode).Any(a => Normalize(a) == given);
    }

    // "return a + b;" -> "a+b". Comparison stays case-sensitive, like Java.
    public static string Normalize(string? code)
    {
        var text = LeadingReturn().Replace((code ?? string.Empty).Trim(), string.Empty);
        return Whitespace().Replace(text, string.Empty).TrimEnd(';');
    }

    // Accepted answers are stored one per line, because code can contain commas
    public static List<string> SplitLines(string? text) =>
        (text ?? string.Empty)
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

    [GeneratedRegex(@"^return\b")]
    private static partial Regex LeadingReturn();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
