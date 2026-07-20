using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace inventory_dashboard.Models
{
    public class Order : BaseEntity
    {
        [Required(ErrorMessage = "Customer ID is required.")]
        public string CustomerId { get; set; } = string.Empty;   // ✅ string, not int

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public string? Notes { get; set; }
        public string? PurchaseRequestNotes { get; set; }

        // No navigation property – no foreign key
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}