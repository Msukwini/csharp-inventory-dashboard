using inventory_dashboard.Models;

namespace inventory_dashboard.Repositories;

public interface IProductRepository
{
    // Interface contract - this demonstrates Java Interface knowledge
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task<IEnumerable<Product>> GetLowStockAsync(int threshold);
    Task<int> GetTotalCountAsync();
}