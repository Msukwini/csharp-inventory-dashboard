using Microsoft.EntityFrameworkCore;
using inventory_dashboard.Data;
using inventory_dashboard.Repositories;
using inventory_dashboard.Hubs;
using inventory_dashboard.Models;
using inventory_dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Remove all configuration sources (so it never looks for appsettings.json)
builder.Configuration.Sources.Clear();

// Add services to the container
builder.Services.AddControllersWithViews();

// --- Add Session Support ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Use SQLite (no external database needed)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=inventory.db"));

// Dependency Injection (Factory Pattern)
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<PdfService>();

// SignalR for real-time alerts
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // <-- Add this line
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<StockHub>("/stockHub");

// Create database and seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    Console.WriteLine("✅ Database created: inventory.db");

    // Seed default customer if none exists
    if (!dbContext.Customers.Any())
    {
        dbContext.Customers.Add(new Customer
        {
            FirstName = "Default",
            LastName = "Customer",
            Email = "default@example.com",
            Phone = "000-000-0000",
            Company = "Default Company"
        });
        dbContext.SaveChanges();
        Console.WriteLine("✅ Default customer seeded (ID = 1).");
    }

    // Seed sample products if none exist
    if (!dbContext.Products.Any())
    {
        dbContext.Products.AddRange(
            new Product { Name = "Coffee Beans", Description = "Premium Arabica", Price = 12.99m, StockQuantity = 25, SKU = "COF-001", Category = "Beverages" },
            new Product { Name = "Espresso Machine", Description = "Professional grade", Price = 299.99m, StockQuantity = 8, SKU = "ESP-001", Category = "Equipment" },
            new Product { Name = "Paper Cups (50pk)", Description = "Eco-friendly", Price = 5.99m, StockQuantity = 45, SKU = "CUP-001", Category = "Supplies" },
            new Product { Name = "Almond Milk", Description = "Unsweetened", Price = 4.99m, StockQuantity = 12, SKU = "ALM-001", Category = "Beverages" },
            new Product { Name = "Vanilla Syrup", Description = "Monin brand", Price = 8.99m, StockQuantity = 5, SKU = "VAN-001", Category = "Flavorings" }
        );
        dbContext.SaveChanges();
        Console.WriteLine("✅ Sample products seeded.");
    }
}

Console.WriteLine("🚀 Inventory Dashboard running at http://0.0.0.0:5000");
app.Run("http://0.0.0.0:5000");
