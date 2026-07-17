using Npgsql;
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

// --- Database Configuration: PostgreSQL (production) or SQLite (local) ---
// Priority: 1. NEON_INVENTORY_DB  2. DATABASE_URL  3. SQLite fallback
var connectionString = Environment.GetEnvironmentVariable("NEON_INVENTORY_DB")
                       ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=inventory.db";

Console.WriteLine($"📦 Using database: {(connectionString.Contains("postgres") ? "PostgreSQL (Neon)" : "SQLite")}");

if (connectionString.Contains("postgres") || connectionString.Contains("postgresql"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}

// --- Dependency Injection (Factory Pattern) ---
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// --- HttpContextAccessor for audit trail (to get current user) ---
builder.Services.AddHttpContextAccessor();

// --- SignalR for real-time alerts ---
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

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<StockHub>("/stockHub");

// --- Create database and seed data ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    Console.WriteLine("✅ Database created/verified.");

    // --- Seed Customers ---
    if (!dbContext.Customers.Any())
    {
        dbContext.Customers.AddRange(
            new Customer { Id = 42, FirstName = "Naledi", LastName = "Khumalo", Email = "naledi.k@example.com", Phone = "082-555-0142", Company = "Khumalo Catering" },
            new Customer { Id = 17, FirstName = "Sipho", LastName = "Dlamini", Email = "sipho.d@example.com", Phone = "083-555-0017", Company = "Dlamini Events" },
            new Customer { Id = 89, FirstName = "Amara", LastName = "van der Merwe", Email = "amara.vdm@example.com", Phone = "084-555-0089", Company = "Sunrise Bistro" },
            new Customer { Id = 33, FirstName = "Thabo", LastName = "Nkosi", Email = "thabo.n@example.com", Phone = "072-555-0033", Company = "Nkosi Wholesale" }
        );
        dbContext.SaveChanges();
        Console.WriteLine("✅ Customers seeded.");
    }

    // --- Seed Products ---
    if (!dbContext.Products.Any())
    {
        dbContext.Products.AddRange(
            new Product { Name = "Arabica Blend Coffee", Description = "Premium single-origin Ethiopian blend", Price = 185.00m, StockQuantity = 42, SKU = "BEV-001", Category = "Beverages" },
            new Product { Name = "Sourdough Loaf", Description = "Freshly baked 48-hour fermented sourdough", Price = 65.00m, StockQuantity = 8, SKU = "BAK-001", Category = "Bakery" },
            new Product { Name = "Almond Croissant", Description = "Buttery layers with almond cream filling", Price = 45.00m, StockQuantity = 5, SKU = "BAK-002", Category = "Bakery" },
            new Product { Name = "Cold Brew Concentrate", Description = "12-hour slow-dripped concentrate, 500ml", Price = 120.00m, StockQuantity = 23, SKU = "BEV-002", Category = "Beverages" },
            new Product { Name = "Chai Spice Blend", Description = "House-blended masala chai spice mix", Price = 95.00m, StockQuantity = 3, SKU = "BEV-003", Category = "Beverages" },
            new Product { Name = "Avocado Toast Kit", Description = "2 portions: sourdough + smashed avo + seeds", Price = 89.00m, StockQuantity = 15, SKU = "FOD-001", Category = "Food" },
            new Product { Name = "Oat Milk (1L)", Description = "Barista-grade oat milk for steaming", Price = 55.00m, StockQuantity = 9, SKU = "DAR-001", Category = "Dairy Alt" },
            new Product { Name = "Granola Pot", Description = "House granola with yoghurt and berries", Price = 72.00m, StockQuantity = 30, SKU = "FOD-002", Category = "Food" }
        );
        dbContext.SaveChanges();
        Console.WriteLine("✅ Products seeded.");
    }

    // --- Seed Orders ---
    if (!dbContext.Orders.Any())
    {
        var coffee = dbContext.Products.First(p => p.SKU == "BEV-001");
        var coldBrew = dbContext.Products.First(p => p.SKU == "BEV-002");
        var chai = dbContext.Products.First(p => p.SKU == "BEV-003");
        var toast = dbContext.Products.First(p => p.SKU == "FOD-001");
        var granola = dbContext.Products.First(p => p.SKU == "FOD-002");
        var today = DateTime.UtcNow.Date;

        dbContext.Orders.AddRange(
            new Order {
                CustomerId = 42, Status = "Approved", TotalAmount = 795.00m,
                OrderDate = today.AddDays(-3), CreatedAt = today.AddDays(-3),
                OrderItems = new List<OrderItem> {
                    new OrderItem { ProductId = coffee.Id, Quantity = 3, Price = coffee.Price },
                    new OrderItem { ProductId = coldBrew.Id, Quantity = 2, Price = coldBrew.Price }
                }
            },
            new Order {
                CustomerId = 17, Status = "Pending", TotalAmount = 685.00m,
                OrderDate = today.AddDays(-2), CreatedAt = today.AddDays(-2),
                OrderItems = new List<OrderItem> {
                    new OrderItem { ProductId = coldBrew.Id, Quantity = 2, Price = coldBrew.Price },
                    new OrderItem { ProductId = toast.Id, Quantity = 5, Price = toast.Price }
                }
            },
            new Order {
                CustomerId = 89, Status = "Rejected", TotalAmount = 950.00m,
                OrderDate = today.AddDays(-2), CreatedAt = today.AddDays(-2),
                OrderItems = new List<OrderItem> {
                    new OrderItem { ProductId = chai.Id, Quantity = 10, Price = chai.Price }
                }
            },
            new Order {
                CustomerId = 33, Status = "Pending", TotalAmount = 788.00m,
                OrderDate = today.AddDays(-1), CreatedAt = today.AddDays(-1),
                OrderItems = new List<OrderItem> {
                    new OrderItem { ProductId = granola.Id, Quantity = 6, Price = granola.Price },
                    new OrderItem { ProductId = toast.Id, Quantity = 4, Price = toast.Price }
                }
            }
        );
        dbContext.SaveChanges();
        Console.WriteLine("✅ Orders seeded (ORD-001 to ORD-004).");
    }
}

Console.WriteLine("🚀 Inventory Dashboard running at http://0.0.0.0:5000");
app.Run("http://0.0.0.0:5000");
