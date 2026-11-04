namespace WebApp.ViewModels;

public class PlayQuestViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public bool Submitted { get; set; }
    public List<PlayQuestionViewModel> Questions { get; set; } = [];
}

public class PlayQuestionViewModel
{
    public int Id { get; set; }
    public string Type { get; set; } = "multipleChoice";
    public string Question { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool AllowMultiple { get; set; }
    public List<PlayOptionViewModel> Options { get; set; } = [];

    // The user's answers
    public List<string> SelectedKeys { get; set; } = [];
    public string? TextAnswer { get; set; }

    // Only applied after quest is submitted
    public bool IsCorrect { get; set; }
    public List<string> CorrectKeys { get; set; } = [];
    public List<string> AcceptedAnswers { get; set; } = [];
    public string? Explanation { get; set; }
}

public class PlayOptionViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class PlayAnswerInput
{
    public int QuestionId { get; set; }
    public List<string> Selected { get; set; } = [];
    public string? Text { get; set; }
}
