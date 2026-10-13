using WebApp.Models;

namespace WebApp.ViewModels;

// The database code should fill these in. Each property is shown on the page.
public class QuestFilterViewModel
{
    public string? SearchText { get; set; }
    public Difficulty? Difficulty { get; set; }
}
