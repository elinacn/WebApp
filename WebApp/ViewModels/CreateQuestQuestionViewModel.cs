namespace WebApp.ViewModels;

// The database code should fill these in. Each property is shown on the page.
public class CreateQuestQuestionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "multipleChoice";
    public string Text { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public List<CreateQuestOptionViewModel> Options { get; set; } =
    [
        new() { Id = "a" },
        new() { Id = "b" },
        new() { Id = "c" },
        new() { Id = "d" }
    ];
    public string CorrectOptionId { get; set; } = "a";
    public string AcceptedAnswers { get; set; } = string.Empty;
    public string Explaination { get; set; } = string.Empty;
}
