using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Dtos;

public record CourseDto(int Id, string Code, string Title, string? Description, int QuestCount);

public record CourseDetailDto(int Id, string Code, string Title, string? Description, List<CourseQuestDto> Quests);

public record CourseQuestDto(int Id, string Title, string Difficulty);

public class CourseFormDto
{
    [Required(ErrorMessage = "Course code is required.")]
    [StringLength(20, ErrorMessage = "Course code can be at most 20 characters.")]
    [RegularExpression(@"^[A-Za-z0-9\- ]+$", ErrorMessage = "Use only letters, numbers, spaces and dashes.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course title is required.")]
    [StringLength(100, ErrorMessage = "Course title can be at most 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description can be at most 500 characters.")]
    public string? Description { get; set; }
}
