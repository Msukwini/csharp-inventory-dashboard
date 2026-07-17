using Microsoft.EntityFrameworkCore;
using inventory_dashboard.Data;
using inventory_dashboard.Repositories;
using inventory_dashboard.Hubs;
using inventory_dashboard.Models;
using inventory_dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Remove all configuration sources
builder.Configuration.Sources.Clear();

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --- Database Configuration ---
string rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                             ?? Environment.GetEnvironmentVariable("NEON_INVENTORY_DB")
                             ?? builder.Configuration.GetConnectionString("DefaultConnection")
                             ?? "Data Source=inventory.db";

Console.WriteLine($"📦 Raw connection string: {rawConnectionString}");

// Convert PostgreSQL URI to ADO.NET format if needed
string connectionString = rawConnectionString;
if (rawConnectionString.StartsWith("postgres://") || rawConnectionString.StartsWith("postgresql://"))
{
    Console.WriteLine("🔄 Converting PostgreSQL URI to ADO.NET format...");
    try
    {
        var uri = new Uri(rawConnectionString);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
        Console.WriteLine($"✅ Converted connection string: {connectionString}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed to parse PostgreSQL URI: {ex.Message}");
        connectionString = "Data Source=inventory.db";
    }
}

Console.WriteLine($"📦 Using database: {(connectionString.Contains("Host=") || connectionString.Contains("postgres") ? "PostgreSQL (Neon)" : "SQLite")}");

// --- Register DbContext with timeout ---
if (connectionString.Contains("Host=") || connectionString.Contains("postgres"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
            npgsqlOptions.CommandTimeout(60))); // 60 seconds timeout
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}

// --- Dependency Injection ---
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();

var app = builder.Build();

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

// --- Database Initialization & Seeding (with retry) ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    bool success = false;
    int retries = 3;
    while (!success && retries > 0)
    {
        try
        {
            Console.WriteLine("🔄 Ensuring database is created...");
            dbContext.Database.EnsureCreated();
            Console.WriteLine("✅ Database created/verified.");

            // Seeding logic
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

            if (!dbContext.Orders.Any())
            {
                var coffee = dbContext.Products.First(p => p.SKU == "BEV-001");
                var coldBrew = dbContext.Products.First(p => p.SKU == "BEV-002");
                var chai = dbContext.Products.First(p => p.SKU == "BEV-003");
                var toast = dbContext.Products.First(p => p.SKU == "FOD-001");
                var granola = dbContext.Products.First(p => p.SKU == "FOD-002");
                var today = DateTime.UtcNow.Date;

                dbContext.Orders.AddRange(
                    new Order { CustomerId = 42, Status = "Approved", TotalAmount = 795.00m, OrderDate = today.AddDays(-3), CreatedAt = today.AddDays(-3), OrderItems = new List<OrderItem> { new OrderItem { ProductId = coffee.Id, Quantity = 3, Price = coffee.Price }, new OrderItem { ProductId = coldBrew.Id, Quantity = 2, Price = coldBrew.Price } } },
                    new Order { CustomerId = 17, Status = "Pending", TotalAmount = 685.00m, OrderDate = today.AddDays(-2), CreatedAt = today.AddDays(-2), OrderItems = new List<OrderItem> { new OrderItem { ProductId = coldBrew.Id, Quantity = 2, Price = coldBrew.Price }, new OrderItem { ProductId = toast.Id, Quantity = 5, Price = toast.Price } } },
                    new Order { CustomerId = 89, Status = "Rejected", TotalAmount = 950.00m, OrderDate = today.AddDays(-2), CreatedAt = today.AddDays(-2), OrderItems = new List<OrderItem> { new OrderItem { ProductId = chai.Id, Quantity = 10, Price = chai.Price } } },
                    new Order { CustomerId = 33, Status = "Pending", TotalAmount = 788.00m, OrderDate = today.AddDays(-1), CreatedAt = today.AddDays(-1), OrderItems = new List<OrderItem> { new OrderItem { ProductId = granola.Id, Quantity = 6, Price = granola.Price }, new OrderItem { ProductId = toast.Id, Quantity = 4, Price = toast.Price } } }
                );
                dbContext.SaveChanges();
                Console.WriteLine("✅ Orders seeded.");
            }

            success = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database operation failed: {ex.Message}");
            retries--;
            if (retries > 0)
            {
                Console.WriteLine($"🔄 Retrying... ({3 - retries} attempts left)");
                Thread.Sleep(2000); // Wait 2 seconds before retrying
            }
            else
            {
                Console.WriteLine("❌ All retries exhausted. Exiting.");
                throw;
            }
        }
    }
}

Console.WriteLine("🚀 Inventory Dashboard running at http://0.0.0.0:5000");
app.Run("http://0.0.0.0:5000");
