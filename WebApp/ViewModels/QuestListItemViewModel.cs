namespace WebApp.ViewModels;

public class QuestListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int TotalPoints { get; set; }
    public DateTime CreatedAt { get; set; }
}