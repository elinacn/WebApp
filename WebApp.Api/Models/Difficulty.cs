namespace WebApp.Api.Models;

public enum Difficulty
{
    Beginner,
    Intermediate,
    Expert,
    Master
}

public static class DifficultyRules
{
    // Accepts any casing and the old names used in the quiz files ("apprentice", "knight").
    // Returns null for an unknown difficulty.
    public static string? Normalize(string? difficulty) => difficulty?.Trim().ToLowerInvariant() switch
    {
        "beginner" or "apprentice" => nameof(Difficulty.Beginner),
        "intermediate" or "knight" => nameof(Difficulty.Intermediate),
        "expert" => nameof(Difficulty.Expert),
        "master" => nameof(Difficulty.Master),
        _ => null
    };

    // XP awarded the first time a user completes a quest of this difficulty
    public static int GetCompletionXp(string difficulty) => Normalize(difficulty) switch
    {
        nameof(Difficulty.Beginner) => 10,
        nameof(Difficulty.Intermediate) => 25,
        nameof(Difficulty.Expert) => 40,
        nameof(Difficulty.Master) => 60,
        _ => 0
    };
}
