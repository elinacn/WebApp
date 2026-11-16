using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Dtos;

// A quest card in lists. BestPercent is the signed-in user's best score (0-100), or null if not completed.
public record QuestSummaryDto(
    int Id,
    string Title,
    string? Description,
    string Difficulty,
    int? CourseId,
    string? CourseCode,
    int QuestionCount,
    int TotalPoints,
    int Xp,
    DateTime CreatedAt,
    int? BestPercent);

// A quest ready to play. Never contains the correct answers.
public record QuestPlayDto(
    int Id,
    string Title,
    string? Description,
    string Difficulty,
    int Xp,
    List<QuestionPlayDto> Questions);

public record QuestionPlayDto(
    int Id,
    string Type,
    string Text,
    int Points,
    bool AllowMultiple,
    string? StartCode,
    List<OptionDto> Options);

public record OptionDto(string Key, string Text);

// Create/edit form. Data annotations are the basic checks; QuestValidator adds the domain rules.
public class QuestFormDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be 2-100 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description can be at most 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose a difficulty.")]
    public string Difficulty { get; set; } = string.Empty;

    public int? CourseId { get; set; }

    public List<QuestionFormDto> Questions { get; set; } = [];
}

public class QuestionFormDto
{
    public string Type { get; set; } = "multipleChoice";

    [Required(ErrorMessage = "Question text is required.")]
    [StringLength(1000, ErrorMessage = "Question text can be at most 1000 characters.")]
    public string Text { get; set; } = string.Empty;

    [Range(0, 1000, ErrorMessage = "Points must be between 0 and 1000.")]
    public int Points { get; set; } = 1;

    // Multiple choice
    public List<OptionFormDto> Options { get; set; } = [];

    // Free text and code: one accepted answer per item
    public List<string> AcceptedAnswers { get; set; } = [];
    public bool CaseSensitive { get; set; }

    // Code: starter code with exactly one ____ blank
    [StringLength(2000, ErrorMessage = "Start code can be at most 2000 characters.")]
    public string? StartCode { get; set; }

    [StringLength(1000, ErrorMessage = "Explanation can be at most 1000 characters.")]
    public string? Explanation { get; set; }
}

public class OptionFormDto
{
    [StringLength(300, ErrorMessage = "An option can be at most 300 characters.")]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}

// Answers sent when submitting a quest. Selected = option keys, Text = free text / code answer.
public record SubmitAnswersDto(List<AnswerDto> Answers);

public record AnswerDto(int QuestionId, string? Text, List<string>? Selected);

// Result of a submitted attempt. Saved is false if the answers were graded but the score couldn't be stored.
public record QuizResultDto(
    int Score,
    int MaxScore,
    int XpAwarded,
    bool FirstCompletion,
    int BestScore,
    int TotalXp,
    bool Saved,
    List<QuestionResultDto> Questions);

public record QuestionResultDto(
    int QuestionId,
    bool IsCorrect,
    List<string> CorrectKeys,
    List<string> AcceptedAnswers,
    string? Explanation);
