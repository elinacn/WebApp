namespace WebApp.ViewModels;

// The database code should fill these in. Each property is shown on the page.
public class QuestsPageViewModel
{
    public string ActiveSection { get; set; } = "library";
    public QuestFilterViewModel Filter { get; set; } = new();
    public CreateQuestViewModel NewQuest { get; set; } = new();
    public List<QuestListItemViewModel> MyQuests { get; set; } = [];
    public List<QuestCardViewModel> ContinueQuests { get; set; } = [];
    public List<QuestCardViewModel> CompletedQuests { get; set; } = [];
    public List<QuestCardViewModel> FeaturedQuests { get; set; } = [];
    public List<QuestCardViewModel> RecommendedQuests { get; set; } = [];
    public List<QuestCardViewModel> SavedQuests { get; set; } = [];
}
