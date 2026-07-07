using inventory_dashboard.Data;
using Microsoft.EntityFrameworkCore;

namespace inventory_dashboard.Services;

public class DashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        var totalProducts = await _context.Products.CountAsync();
        var lowStock = await _context.Products.CountAsync(p => p.StockQuantity <= 10);
        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
        
        // Total revenue from approved orders
        var orderRevenue = await _context.Orders
            .Where(o => o.Status == "Approved")
            .SumAsync(o => o.TotalAmount);
        
        // Total stock value: sum of (Price * StockQuantity) for all products
        var stockValue = await _context.Products
            .SumAsync(p => p.Price * p.StockQuantity);

        var startDate = DateTime.UtcNow.AddDays(-7);
        var ordersByDay = await _context.Orders
            .Where(o => o.OrderDate >= startDate)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return new DashboardData
        {
            TotalProducts = totalProducts,
            LowStockCount = lowStock,
            PendingOrders = pendingOrders,
            OrderRevenue = orderRevenue,
            StockValue = stockValue,
            WeeklyOrders = ordersByDay.ToDictionary(x => x.Date.ToString("MM/dd"), x => x.Count)
        };
    }
}

public class DashboardData
{
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int PendingOrders { get; set; }
    public decimal OrderRevenue { get; set; }
    public decimal StockValue { get; set; }
    public Dictionary<string, int> WeeklyOrders { get; set; }
}
