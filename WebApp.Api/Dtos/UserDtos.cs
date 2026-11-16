using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Dtos;

public record UserDto(
    int Id,
    string Username,
    string Name,
    string StudyProgram,
    string Role,
    int Points,
    int QuizzesCompleted,
    DateTime CreatedAt);

// Shared fields of the create and edit forms
public abstract class UserFormDto
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be 3-30 characters.")]
    [RegularExpression(@"^[A-Za-z0-9_.-]+$", ErrorMessage = "Use only letters, numbers, dots, dashes and underscores.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(50, ErrorMessage = "Name can be at most 50 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Program is required.")]
    [StringLength(100, ErrorMessage = "Program can be at most 100 characters.")]
    public string StudyProgram { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a role.")]
    public string Role { get; set; } = "Student";
}

public class UserCreateDto : UserFormDto
{
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be 8-100 characters.")]
    public string Password { get; set; } = string.Empty;
}

public class UserUpdateDto : UserFormDto
{
    // Leave empty to keep the current password
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be 8-100 characters.")]
    public string? NewPassword { get; set; }
}

public class LoginDto
{
    [Required(ErrorMessage = "Enter your username.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    public string Password { get; set; } = string.Empty;
}

public record LeaderboardEntryDto(int Rank, int UserId, string Name, string StudyProgram, int Points, int QuizzesCompleted);

public record ProfileDto(UserDto User, int Streak, List<bool> Week, List<ActivityDto> RecentActivity);

public record ActivityDto(int QuestId, string QuestTitle, int BestScore, int MaxScore, int Attempts, DateTime LastCompletedAt);
