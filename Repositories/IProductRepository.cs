using inventory_dashboard.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace inventory_dashboard.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product, string? reason = null);
        Task DeleteAsync(int id);
        Task<IEnumerable<Product>> GetLowStockAsync(int threshold);
        Task<int> GetTotalCountAsync();
        Task<IEnumerable<StockHistory>> GetStockHistoryAsync(int productId);
    }
}
