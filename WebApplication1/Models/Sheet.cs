using System;
namespace WebApplication1.Models
{
    public class Sheet
    {
        public int SheetId { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}