using inventory_dashboard.Models;

namespace inventory_dashboard.Repositories;

public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(int id);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(int id);
    Task<IEnumerable<Order>> GetOrdersByCustomerAsync(string customerId);
    Task<Order> AddOrderWithItemsAsync(Order order, List<OrderItem> items, string purchaseNotes);
}