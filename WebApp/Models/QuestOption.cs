using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class QuestOption
{
    public int Id { get; set; }
    public int QuestQuestionId { get; set; }

    [Required, StringLength(10)]
    public string OptionKey { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Text { get; set; } = string.Empty;
}
