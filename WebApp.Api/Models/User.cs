using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    // The educational program the student belongs to, e.g. "Web programming"
    [Required, StringLength(100)]
    public string StudyProgram { get; set; } = string.Empty;

    // Roles.Admin can manage quests, courses and users; Roles.Student can only play
    [Required, StringLength(20)]
    public string Role { get; set; } = Roles.Student;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Total XP from completed quests (see QuestScoringService)
    public int Points { get; set; }
    public int QuizzesCompleted { get; set; }

    // Hashed with ASP.NET Core's PasswordHasher; the plain password is never stored
    public string PasswordHash { get; set; } = string.Empty;

    // Quests this user completed (many-to-many through QuestCompletion)
    public List<QuestCompletion> Completions { get; set; } = [];
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Student = "Student";

    public static readonly string[] All = [Admin, Student];
}
