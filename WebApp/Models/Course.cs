using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class Course
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Course code is required.")]
    [StringLength(20, ErrorMessage = "Course code can be at most 20 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course title is required.")]
    [StringLength(100, ErrorMessage = "Course title can be at most 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description can be at most 500 characters.")]
    public string? Description { get; set; }

    public List<Quest> Quests { get; set; } = [];
}
