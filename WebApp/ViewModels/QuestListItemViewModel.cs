using WebApp.Models;

namespace WebApp.ViewModels;

public class QuestListItemViewModel
{
    public int Id { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int XpReward => DifficultyRules.GetCompletionXp(Difficulty);
    public int QuestionCount { get; set; }
    public int TotalPoints { get; set; }
    public DateTime CreatedAt { get; set; }
}