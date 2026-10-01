using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class Sheet
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be 2–100 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(1, 1000, ErrorMessage = "Row count must be between 1 and 1000.")]
    [Display(Name = "Row count")]
    public int RowCount { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Created")]
    public DateTime CreatedAt { get; set; } = DateTime.Today;
}