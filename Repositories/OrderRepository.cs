using inventory_dashboard.Data;
using inventory_dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_dashboard.Repositories;

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

    public async Task AddAsync(Order order)
    {
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerAsync(int customerId)
    {
        return await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<Order> AddOrderWithItemsAsync(Order order, List<OrderItem> items, string purchaseNotes)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Save the order first
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Add items with the generated OrderId
            foreach (var item in items)
            {
                item.OrderId = order.Id;
                item.CreatedAt = DateTime.UtcNow;
                item.UpdatedAt = DateTime.UtcNow;
                await _context.OrderItems.AddAsync(item);
            }

            // Save purchase notes if any
            if (!string.IsNullOrWhiteSpace(purchaseNotes))
            {
                order.PurchaseRequestNotes = purchaseNotes;
                _context.Orders.Update(order);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return order;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"❌ Transaction failed: {ex.Message}");
            throw; // rethrow so the controller catches it
        }
    }
}