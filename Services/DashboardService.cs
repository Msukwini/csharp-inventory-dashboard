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
        var orderRevenue = await _context.Orders
            .Where(o => o.Status == "Approved")
            .SumAsync(o => o.TotalAmount);
        var stockValue = await _context.Products
            .SumAsync(p => p.Price * p.StockQuantity);

        // Calculate total items (sum of all stock quantities)
        var totalItems = await _context.Products.SumAsync(p => p.StockQuantity);

        // Weekly orders (last 7 days)
        var startDate = DateTime.UtcNow.AddDays(-7);
        var ordersByDay = await _context.Orders
            .Where(o => o.OrderDate >= startDate)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var weeklyOrders = ordersByDay.ToDictionary(x => x.Date.ToString("MM/dd"), x => x.Count);

        return new DashboardData
        {
            TotalProducts = totalProducts,
            LowStockCount = lowStock,
            PendingOrders = pendingOrders,
            OrderRevenue = orderRevenue,
            StockValue = stockValue,
            TotalItems = totalItems,
            WeeklyOrders = weeklyOrders
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
    public int TotalItems { get; set; }
    public Dictionary<string, int> WeeklyOrders { get; set; } = new();
}
