using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Models;

// One answer option of a multiple choice question, keyed "a", "b", "c" ...
public class QuestOption
{
    public int Id { get; set; }
    public int QuestQuestionId { get; set; }

    [Required, StringLength(10)]
    public string Key { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Text { get; set; } = string.Empty;
}
