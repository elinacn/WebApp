using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Models;

// A quiz made of questions. Students earn XP the first time they complete it.
public class Quest
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // Stored as the Difficulty enum's name, e.g. "Beginner"
    [Required, StringLength(30)]
    public string Difficulty { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional: a quest can exist without a course
    public int? CourseId { get; set; }
    public Course? Course { get; set; }

    public List<QuestQuestion> Questions { get; set; } = [];

    // Users who completed this quest (many-to-many through QuestCompletion)
    public List<QuestCompletion> Completions { get; set; } = [];
}
