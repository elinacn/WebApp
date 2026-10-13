namespace WebApp.Models;

// The database code should fill these in. Each property is shown on the page.
public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
