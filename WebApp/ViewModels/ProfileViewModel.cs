namespace WebApp.ViewModels;

public class ProfileViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int Streak { get; set; }
    public int TotalXp { get; set; }
    public int QuizzesCompleted { get; set; }
    // The challenge feature is not implemented yet.
    public int ChallengesCompleted => 0;

    // Existing code points are XP. Each 100 XP advances one level, starting at level 1.
    public const int XpPerLevel = 100;
    public int Level => TotalXp / XpPerLevel + 1;
    public int XpInCurrentLevel => TotalXp % XpPerLevel;
    public int XpToNextLevel => XpPerLevel - XpInCurrentLevel;
    public List<ProfileActivityViewModel> RecentActivity { get; set; } = [];

    public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2).Select(part => char.ToUpperInvariant(part[0])));
}

public class ProfileActivityViewModel
{
    public string QuestTitle { get; set; } = string.Empty;
    public int BestScore { get; set; }
    public int MaxScore { get; set; }
    public int Attempts { get; set; }
    public DateTime CompletedAtUtc { get; set; }

    public DateTime CompletedAtOslo => TimeZoneInfo.ConvertTimeFromUtc(
        DateTime.SpecifyKind(CompletedAtUtc, DateTimeKind.Utc),
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo"));
}
