namespace WebApp.Models;

public static class DifficultyRules
{
    // Accept the old names for imported quests, but always store/display the new names.
    public static string Normalize(string difficulty) => difficulty.Trim().ToLowerInvariant() switch
    {
        "beginner" or "apprentice" => nameof(Difficulty.Beginner),
        "intermediate" or "knight" => nameof(Difficulty.Intermediate),
        "expert" => nameof(Difficulty.Expert),
        "master" => nameof(Difficulty.Master),
        _ => throw new ArgumentException("Unknown quest difficulty.", nameof(difficulty))
    };

    public static int GetCompletionXp(string difficulty) => Normalize(difficulty) switch
    {
        nameof(Difficulty.Beginner) => 10,
        nameof(Difficulty.Intermediate) => 25,
        nameof(Difficulty.Expert) => 40,
        nameof(Difficulty.Master) => 60,
        _ => throw new ArgumentException("Unknown quest difficulty.", nameof(difficulty))
    };
}
