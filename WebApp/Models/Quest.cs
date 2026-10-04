using System.ComponentModel.DataAnnotations;
using WebApp.ViewModels;

namespace WebApp.Models;

// The database code should fill these in. just barebones right now
public class Quest
{
    public int Id { get; set; }
    
    [Required, StringLength(100, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, StringLength(30)]
    public string Difficulty { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Today;

    public List<QuestQuestion> Questions { get; set; } = [];
}
