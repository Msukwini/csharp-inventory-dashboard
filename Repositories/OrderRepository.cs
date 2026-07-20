using inventory_dashboard.Models;
using inventory_dashboard.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace inventory_dashboard.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerAsync(string customerId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task AddAsync(Order order)
        {
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }

        public async Task<Order> AddOrderWithItemsAsync(Order order, List<OrderItem> items, string purchaseRequestNotes)
        {
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            // ✅ Set timestamps on each order item
            foreach (var item in items)
            {
                // Ensure the item has the order ID set (EF will set it after insert)
                // But we need to set CreatedAt/UpdatedAt if they exist on OrderItem
                // If OrderItem inherits from BaseEntity, it has those properties
                // Set them to avoid null constraint errors.
                // If your OrderItem does NOT have CreatedAt/UpdatedAt, you can skip this.
                // But the error shows the column exists, so we'll set them.
                // We'll assume OrderItem has CreatedAt and UpdatedAt (maybe from BaseEntity).
                // If not, you may need to add them to the model.
                // For safety, we'll set them if the property exists.
                var itemType = item.GetType();
                var createdAtProp = itemType.GetProperty("CreatedAt");
                var updatedAtProp = itemType.GetProperty("UpdatedAt");
                if (createdAtProp != null && createdAtProp.CanWrite)
                    createdAtProp.SetValue(item, DateTime.UtcNow);
                if (updatedAtProp != null && updatedAtProp.CanWrite)
                    updatedAtProp.SetValue(item, DateTime.UtcNow);

                // Also ensure the product is attached to avoid duplicates
                if (item.Product != null)
                    _context.Products.Attach(item.Product);
            }

            order.OrderItems = items;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task UpdateAsync(Order order)
        {
            order.UpdatedAt = DateTime.UtcNow;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }
    }
}