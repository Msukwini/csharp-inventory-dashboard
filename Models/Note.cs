using System;

namespace inventory_dashboard.Models
{
    public class Note
    {
        public int Id { get; set; }
        public int? ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public bool IsCompleted { get; set; }
        public bool IsDeleted { get; set; } = false;   // <-- NEW
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        public virtual Product? Product { get; set; }
    }
}