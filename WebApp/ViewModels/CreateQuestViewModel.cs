using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

public class CreateQuestViewModel
{
    public int Id { get; set; }
    public string QuestType { get; set; } = "quest";

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be between 2-100 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description can be at most 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Difficulty is required.")]
    [StringLength(30, ErrorMessage = "Difficulty can be at most 30 characters.")]
    public string Difficulty { get; set; } = string.Empty;

    public List<CreateQuestQuestionViewModel> Questions { get; set; } =
    [
        new()
    ];
}
