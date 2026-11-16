using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Models;

// One question in a quest. Which fields are used depends on Type:
// - "multipleChoice": Options + CorrectOptionKeys
// - "freeText":       AcceptedAnswers (+ CaseSensitive)
// - "code":           StartCode with one ____ blank + AcceptedAnswers for the blank
public class QuestQuestion
{
    public int Id { get; set; }
    public int QuestId { get; set; }
    public int Position { get; set; }

    [Required, StringLength(20)]
    public string Type { get; set; } = QuestionTypes.MultipleChoice;

    [Required, StringLength(1000)]
    public string Text { get; set; } = string.Empty;

    public int Points { get; set; } = 1;

    // Multiple choice: comma-separated keys of the correct options, e.g. "a" or "a,c"
    [StringLength(20)]
    public string? CorrectOptionKeys { get; set; }

    // Free text and code: accepted answers, one per line (code can contain commas)
    [StringLength(1000)]
    public string? AcceptedAnswers { get; set; }

    public bool CaseSensitive { get; set; }

    [StringLength(2000)]
    public string? StartCode { get; set; }

    [StringLength(1000)]
    public string? Explanation { get; set; }

    public List<QuestOption> Options { get; set; } = [];
}

public static class QuestionTypes
{
    public const string MultipleChoice = "multipleChoice";
    public const string FreeText = "freeText";
    public const string Code = "code";

    public static readonly string[] All = [MultipleChoice, FreeText, Code];
}
