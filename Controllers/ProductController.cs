using inventory_dashboard.Models;
using inventory_dashboard.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using inventory_dashboard.Hubs;

namespace inventory_dashboard.Controllers;

public class ProductController : BaseController
{
    private readonly IProductRepository _productRepo;
    private readonly IHubContext<StockHub> _hubContext;

    public ProductController(IProductRepository productRepo, IHubContext<StockHub> hubContext)
    {
        _productRepo = productRepo;
        _hubContext = hubContext;
    }

    // GET: /Product (with search/filter)
    public async Task<IActionResult> Index(string searchTerm, string category, int? minStock, int? maxStock)
    {
        var products = await _productRepo.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            products = products.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                           p.SKU.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(category))
            products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        if (minStock.HasValue)
            products = products.Where(p => p.StockQuantity >= minStock.Value);
        if (maxStock.HasValue)
            products = products.Where(p => p.StockQuantity <= maxStock.Value);

        var allProducts = await _productRepo.GetAllAsync();
        ViewBag.Categories = allProducts.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
        ViewBag.CurrentSearchTerm = searchTerm;
        ViewBag.CurrentCategory = category;
        ViewBag.CurrentMinStock = minStock;
        ViewBag.CurrentMaxStock = maxStock;

        var lowStock = await _productRepo.GetLowStockAsync(10);
        ViewBag.LowStockCount = lowStock.Count();
        ViewBag.TotalProducts = products.Count();

        return View(products);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (ModelState.IsValid)
        {
            await _productRepo.AddAsync(product);
            if (product.StockQuantity <= 10)
                await _hubContext.Clients.All.SendAsync("ReceiveAlert", product.Name, product.StockQuantity);
            Console.WriteLine($"✅ Product created: {product.Name} (SKU: {product.SKU})");
            return RedirectToAction(nameof(Index));
        }
        else
        {
            Console.WriteLine("❌ Product Create - ModelState invalid:");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                Console.WriteLine($"   {error.ErrorMessage}");
        }
        return View(product);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product)
    {
        if (id != product.Id) return NotFound();

        if (ModelState.IsValid)
        {
            await _productRepo.UpdateAsync(product);
            if (product.StockQuantity <= 10)
                await _hubContext.Clients.All.SendAsync("ReceiveAlert", product.Name, product.StockQuantity);
            Console.WriteLine($"✅ Product updated: {product.Name}");
            return RedirectToAction(nameof(Index));
        }
        else
        {
            Console.WriteLine("❌ Product Edit - ModelState invalid:");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                Console.WriteLine($"   {error.ErrorMessage}");
        }
        return View(product);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _productRepo.DeleteAsync(id);
        Console.WriteLine($"🗑️ Product deleted: ID {id}");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> LowStock()
    {
        var products = await _productRepo.GetLowStockAsync(10);
        ViewBag.TotalProducts = await _productRepo.GetTotalCountAsync();
        return View(products);
    }

    public async Task<IActionResult> Restock(int id)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restock(Product product)
    {
        var existing = await _productRepo.GetByIdAsync(product.Id);
        if (existing == null) return NotFound();

        existing.StockQuantity = product.StockQuantity;
        existing.Price = product.Price;
        existing.UpdatedAt = DateTime.UtcNow;

        await _productRepo.UpdateAsync(existing);

        if (existing.StockQuantity <= 10)
            await _hubContext.Clients.All.SendAsync("ReceiveAlert", existing.Name, existing.StockQuantity);

        Console.WriteLine($"✅ Product restocked: {existing.Name} (Stock: {existing.StockQuantity}, Price: {existing.Price})");
        return RedirectToAction(nameof(LowStock));
    }
}
