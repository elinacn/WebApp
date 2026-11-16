using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Models;

// Groups related quests, e.g. "WEB-101 Web programming"
public class Course
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public List<Quest> Quests { get; set; } = [];
}
