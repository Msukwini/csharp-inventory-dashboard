using inventory_dashboard.Models;
using inventory_dashboard.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace inventory_dashboard.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductRepository(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task AddAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product, string? reason = null)
        {
            var existing = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            if (existing == null)
                throw new InvalidOperationException($"Product with ID {product.Id} not found.");

            if (existing.StockQuantity != product.StockQuantity)
            {
                var history = new StockHistory
                {
                    ProductId = product.Id,
                    PreviousStock = existing.StockQuantity,
                    NewStock = product.StockQuantity,
                    Reason = string.IsNullOrEmpty(reason) ? "Manual update" : reason,
                    ChangedBy = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "System",
                    ChangedAt = DateTime.UtcNow
                };
                _context.StockHistories.Add(history);
            }

            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Product>> GetLowStockAsync(int threshold)
        {
            return await _context.Products
                .Where(p => p.StockQuantity <= threshold)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Products.CountAsync();
        }

        // ✅ NEW: Get stock history for a product
        public async Task<IEnumerable<StockHistory>> GetStockHistoryAsync(int productId)
        {
            return await _context.StockHistories
                .Where(h => h.ProductId == productId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }
    }
}