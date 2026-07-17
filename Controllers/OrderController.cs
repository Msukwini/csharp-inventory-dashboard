using inventory_dashboard.Models;
using inventory_dashboard.Repositories;
using inventory_dashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace inventory_dashboard.Controllers;

public class OrderController : BaseController
{
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly PdfService _pdfService;

    public OrderController(IOrderRepository orderRepo, IProductRepository productRepo, PdfService pdfService)
    {
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _pdfService = pdfService;
    }

    private bool IsAdmin()
    {
        return HttpContext.Session.GetString("IsAdmin") == "true";
    }

    // GET: /Order
    public async Task<IActionResult> Index()
    {
        var orders = await _orderRepo.GetAllAsync();
        return View(orders);
    }

    // GET: /Order/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderRepo.GetByIdAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    // GET: /Order/Create
    public async Task<IActionResult> Create()
    {
        var products = await _productRepo.GetAllAsync();
        ViewBag.Products = products;
        return View();
    }

    public async Task<IActionResult> ExportCsv()
    {
        var orders = await _orderRepo.GetAllAsync();
        var csv = "OrderId,CustomerId,Date,Total,Status,Items\n";
        foreach (var o in orders)
        {
            var itemCount = o.OrderItems?.Count ?? 0;
            csv += $"{o.Id},{o.CustomerId},{o.OrderDate:yyyy-MM-dd},{o.TotalAmount},{o.Status},{itemCount}\n";
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"Orders_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    // POST: /Order/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Order order, List<OrderItem> items, string purchaseNotes, string? otherProductName, int? otherProductQuantity, decimal? otherProductPrice)
    {
        items ??= new List<OrderItem>();

        bool hasItems = items.Any(i => i.ProductId > 0 && i.Quantity > 0);
        bool hasPurchaseRequest = !string.IsNullOrWhiteSpace(otherProductName) && otherProductQuantity.HasValue && otherProductQuantity > 0;

        if (!hasItems && !hasPurchaseRequest)
        {
            ModelState.AddModelError("", "Please add at least one product or fill in the purchase request.");
            ViewBag.Products = await _productRepo.GetAllAsync();
            return View(order);
        }

        decimal total = 0;
        var validItems = new List<OrderItem>();

        if (hasItems)
        {
            foreach (var item in items.Where(i => i.ProductId > 0 && i.Quantity > 0))
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", $"Product with ID {item.ProductId} not found.");
                    ViewBag.Products = await _productRepo.GetAllAsync();
                    return View(order);
                }
                if (item.Price == 0) item.Price = product.Price;
                item.Quantity = item.Quantity > 0 ? item.Quantity : 1;
                total += item.Price * item.Quantity;
                validItems.Add(item);
            }
        }

        string purchaseRequestText = "";
        if (hasPurchaseRequest)
        {
            purchaseRequestText = $"{otherProductName} (Qty: {otherProductQuantity})";
            if (otherProductPrice.HasValue && otherProductPrice.Value > 0)
                purchaseRequestText += $" Price: {otherProductPrice.Value:C}";
            purchaseRequestText += " - Requested from supplier.";
            order.PurchaseRequestNotes = purchaseRequestText;
        }

        order.TotalAmount = total;
        order.OrderDate = DateTime.UtcNow;
        order.Status = hasItems ? "Pending" : "Purchase Request";
        order.Notes = purchaseNotes;

        try
        {
            if (validItems.Any())
                await _orderRepo.AddOrderWithItemsAsync(order, validItems, purchaseRequestText);
            else
                await _orderRepo.AddAsync(order);

            Console.WriteLine($"✅ Order created successfully. ID: {order.Id}");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR saving order: {ex.Message}");
            ModelState.AddModelError("", $"Error saving order: {ex.Message}");
            ViewBag.Products = await _productRepo.GetAllAsync();
            return View(order);
        }
    }

    // GET: /Order/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var order = await _orderRepo.GetByIdAsync(id);
        if (order == null) return NotFound();

        if (order.Status == "Approved")
        {
            TempData["ErrorMessage"] = "Approved orders cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var hoursSinceCreation = (DateTime.UtcNow - order.CreatedAt).TotalHours;
        if (hoursSinceCreation > 1)
        {
            TempData["ErrorMessage"] = "This order is older than 1 hour and can no longer be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ViewBag.Products = await _productRepo.GetAllAsync();
        return View(order);
    }

    // POST: /Order/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Order order, List<OrderItem> items)
    {
        if (id != order.Id) return NotFound();

        var existing = await _orderRepo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (existing.Status == "Approved")
        {
            TempData["ErrorMessage"] = "Approved orders cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var hoursSinceCreation = (DateTime.UtcNow - existing.CreatedAt).TotalHours;
        if (hoursSinceCreation > 1)
        {
            TempData["ErrorMessage"] = "This order is older than 1 hour and can no longer be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        existing.Status = order.Status;
        existing.Notes = order.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _orderRepo.UpdateAsync(existing);
        Console.WriteLine($"✅ Order updated: ID {order.Id}");
        return RedirectToAction(nameof(Index));
    }

    // POST: /Order/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var order = await _orderRepo.GetByIdAsync(id);
        if (order == null) return NotFound();

         if (order.Status != "Pending")
        {
             TempData["ErrorMessage"] = "Only pending orders can be approved.";
             return RedirectToAction(nameof(Details), new { id });
        }

         foreach (var item in order.OrderItems)
        {
             var product = await _productRepo.GetByIdAsync(item.ProductId);
             if (product != null)
            {
                var newStock = product.StockQuantity - item.Quantity;
                if (newStock < 0)
                {
                     TempData["ErrorMessage"] = $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, requested: {item.Quantity}.";
                     return RedirectToAction(nameof(Details), new { id });
                }

                 product.StockQuantity = newStock;

                 // Pass reason with order ID
                 await _productRepo.UpdateAsync(product, $"Order #{order.Id} approved - {item.Quantity} units removed");
            }
        }

        order.Status = "Approved";
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepo.UpdateAsync(order);

       // Fire confetti via SignalR? Already handled in view.

        return RedirectToAction(nameof(Details), new { id });
    }
    


    // POST: /Order/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        if (!IsAdmin())
        {
            TempData["ErrorMessage"] = "You must be logged in as Admin to reject orders.";
            return RedirectToAction("Login", "Login");
        }

        var order = await _orderRepo.GetByIdAsync(id);
        if (order == null) return NotFound();

        order.Status = "Rejected";
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepo.UpdateAsync(order);
        Console.WriteLine($"❌ Order rejected: ID {id}");
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Order/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _orderRepo.GetByIdAsync(id);
        if (order == null) return NotFound();

        if (order.Status != "Approved")
        {
            TempData["ErrorMessage"] = "Only pending orders can be deleted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(order);
    }

    // POST: /Order/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!IsAdmin())
        {
            TempData["ErrorMessage"] = "You must be logged in as Admin to delete orders.";
            return RedirectToAction("Login", "Login");
        }

        var order = await _orderRepo.GetByIdAsync(id);
        if (order == null) return NotFound();

        if (order.Status != "Pending")
        {
            TempData["ErrorMessage"] = "Only pending orders can be deleted.";
            return RedirectToAction(nameof(Index));
        }

        await _orderRepo.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }


    // GET: /Order/DownloadInvoice/5
    public async Task<IActionResult> DownloadInvoice(int id)
    {
        var order = await _orderRepo.GetByIdAsync(id);
        if (order == null) return NotFound();

        var pdfBytes = _pdfService.GenerateOrderInvoice(order);
        return File(pdfBytes, "application/pdf", $"Invoice_Order_{order.Id}.pdf");
    }
}
