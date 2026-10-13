using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

public class CreateQuestQuestionViewModel
{
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
        new() { Id = "b" },
        new() { Id = "c" },
        new() { Id = "d" }
    ];

    [StringLength(10)]
    public string? CorrectOptionId { get; set; } = "a";

    [StringLength(500, ErrorMessage = "Accepted answers can be at most 500 characters.")]
    public string? AcceptedAnswers { get; set; }

    [StringLength(1000, ErrorMessage = "Explaination can be at most 1000 characters.")]
    public string? Explaination { get; set; }
}