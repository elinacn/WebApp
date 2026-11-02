namespace WebApp.ViewModels;

public class LeaderboardViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public List<string> Courses { get; set; } = [];
    public List<LeaderboardEntryViewModel> Entries { get; set; } = [];
}

public class LeaderboardEntryViewModel
{
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public int Points { get; set; }
    public int QuizzesCompleted { get; set; }
}
