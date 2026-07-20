using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace inventory_dashboard.Models
{
    public class Order : BaseEntity
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public string? Notes { get; set; }
        public string? PurchaseRequestNotes { get; set; }

        // Navigation properties – nullable to avoid validation errors
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}