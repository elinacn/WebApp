using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class QuestQuestion
{
    public int Id { get; set; }
    public int QuestId { get; set; }
    public int Position { get; set; }

    [Required, StringLength(20)]
    public string Type { get; set; } = "multipleChoice";

    [Required, StringLength(1000)]
    public string Question { get; set; } = string.Empty;

    public int Points { get; set; } = 1;

    [StringLength(10)]
    public string? CorrectOptionId { get; set; }

    [StringLength(500)]
    public string? AcceptedAnswers { get; set; }

    [StringLength(1000)]
    public string? Explaination { get; set; }

    public List<QuestOption> Options { get; set; } = [];
}
