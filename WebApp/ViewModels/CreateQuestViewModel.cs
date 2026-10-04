namespace WebApp.ViewModels;

// The database code should fill these in. Each property is shown on the page.
public class CreateQuestViewModel
{
    public string QuestType { get; set; } = "quiz";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<CreateQuestQuestionViewModel> Questions { get; set; } =
    [
        new()
    ];
}
