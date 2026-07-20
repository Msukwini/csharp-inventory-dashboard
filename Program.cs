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
                );
                dbContext.SaveChanges();
                Console.WriteLine("✅ Customers seeded.");
            }

            if (!dbContext.Products.Any())
            {
                dbContext.Products.AddRange(
                    
                
                );
                dbContext.SaveChanges();
                Console.WriteLine("✅ Products seeded.");
            }

            if (!dbContext.Orders.Any())
            {
            
                var today = DateTime.UtcNow.Date;

                dbContext.Orders.AddRange(
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

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
Console.WriteLine($"🚀 Inventory Dashboard running on port {port}");
app.Run($"http://0.0.0.0:{port}");
