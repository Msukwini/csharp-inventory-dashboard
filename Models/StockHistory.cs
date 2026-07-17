using System;

namespace inventory_dashboard.Models
{
    public class StockHistory
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public string Reason { get; set; } = string.Empty; // e.g., "Order #42 approved", "Manual restock"
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Product? Product { get; set; }
    }
}