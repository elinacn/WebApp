namespace WebApp.ViewModels;

public class LeaderboardViewModel
{
    public string Search { get; set; } = string.Empty;
    public List<LeaderboardEntryViewModel> Entries { get; set; } = [];
}

public class LeaderboardEntryViewModel
{
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Points { get; set; }
    public int QuizzesCompleted { get; set; }
}
