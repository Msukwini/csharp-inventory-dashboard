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
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<PdfService>();

// Add HttpContextAccessor for audit trail (to get current user)
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuditService, AuditService>();

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

app.UseSession();
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
            new Product { Name = " oGqwe", Description = "amakhekhe", Price = 1.00m, StockQuantity = 42, SKU = "BEV-001", Category = "Cakes" }
        );
        dbContext.SaveChanges();
        Console.WriteLine("✅ Products seeded.");
    }

    if (!dbContext.Orders.Any())
    {
        var coffee = dbContext.Products.First(p => p.SKU == "BEV-001");
        var today = DateTime.UtcNow.Date;

        dbContext.Orders.AddRange(
            
        );
        dbContext.SaveChanges();
        Console.WriteLine("There are no Current Odders");
    }
}

Console.WriteLine("🚀 Inventory Dashboard running at http://0.0.0.0:5000");
app.Run("http://0.0.0.0:5000");