using System.ComponentModel.DataAnnotations;

namespace inventory_dashboard.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }

        // Navigation properties – nullable
        public virtual Order? Order { get; set; }
        public virtual Product? Product { get; set; }
    }
}