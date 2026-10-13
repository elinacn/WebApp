using WebApp.Models;

namespace WebApp.ViewModels;

// The database code should fill these in. Each property is shown on the page.
public class QuestCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
    public int Points { get; set; }
    public QuestStatus Status { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsSaved { get; set; }
}
