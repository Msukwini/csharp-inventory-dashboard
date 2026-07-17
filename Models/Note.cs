using System;

namespace inventory_dashboard.Models
{
    public class Note
    {
        public int Id { get; set; }

        // Nullable – if null, it's a general note; otherwise linked to a product
        public int? ProductId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        // e.g., "Expiry", "Quality", "Restock", "Supplier", "General"
        public string Category { get; set; } = "General";

        // If true, this acts as a "Soft Delete" flag for expired products
        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation property (if you want to link to Product)
        public virtual Product? Product { get; set; }
    }
}