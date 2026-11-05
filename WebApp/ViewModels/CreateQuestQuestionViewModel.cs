using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

public class CreateQuestQuestionViewModel
{
    // Multiple choice: starts with 2 options, the user can add up to 6
    public const int MinOptions = 2;
    public const int MaxOptions = 6;

    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "multipleChoice";

    [Required(ErrorMessage = "Question text is required.")]
    [StringLength(1000, ErrorMessage = "Question text can be at most 1000 characters.")]
    public string Question { get; set; } = string.Empty;

    [Range(0, 1000, ErrorMessage = "Points must be between 0 and 1000.")]
    public int Points { get; set; } = 1;

    public List<CreateQuestOptionViewModel> Options { get; set; } =
    [
        new() { Id = "a" },
        new() { Id = "b" }
    ];

    public List<string> CorrectOptionIds { get; set; } = [];

    // Free text: One correct answer per answer box
    public bool CaseSensitive { get; set; }
    public List<string?> AcceptedAnswers { get; set; } = [""];

    [StringLength(1000, ErrorMessage = "Explaination can be at most 1000 characters.")]
    public string? Explaination { get; set; }
}
