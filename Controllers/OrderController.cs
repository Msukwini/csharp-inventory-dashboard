using inventory_dashboard.Models;
using inventory_dashboard.Repositories;
using inventory_dashboard.Services;
using inventory_dashboard.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace inventory_dashboard.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IProductRepository _productRepo;
        private readonly IAuditService _auditService;
        private readonly AppDbContext _context;

        public OrderController(IOrderRepository orderRepo, IProductRepository productRepo, IAuditService auditService, AppDbContext context)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _auditService = auditService;
            _context = context;
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
            ViewBag.Products = await _productRepo.GetAllAsync();
            return View();
        }

        // POST: /Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Order order,
            List<OrderItem> items,
            string? otherProductName,
            string? otherProductQuantity,
            string? otherProductPrice)
        {
            // --- 🔍 Log model state errors for debugging ---
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state.Errors.Any())
                {
                    Console.WriteLine($"🔍 Key: {key}, Errors: {string.Join(", ", state.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            // --- 🛡️ Ensure CustomerId has a value (fallback to "GUEST") ---
            if (string.IsNullOrWhiteSpace(order.CustomerId))
            {
                order.CustomerId = "GUEST";
            }

            // --- ✅ Parse purchase request fields (they come as strings) ---
            int? parsedQuantity = null;
            decimal? parsedPrice = null;
            if (!string.IsNullOrWhiteSpace(otherProductQuantity) && int.TryParse(otherProductQuantity, out int qty))
                parsedQuantity = qty;
            if (!string.IsNullOrWhiteSpace(otherProductPrice) && decimal.TryParse(otherProductPrice, out decimal price))
                parsedPrice = price;

            // --- Validate after fallback ---
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                Console.WriteLine($"❌ Order creation failed - ModelState invalid: {errors}");
                ViewBag.Products = await _productRepo.GetAllAsync();
                return View(order);
            }

            items ??= new List<OrderItem>();

            bool hasItems = items.Any(i => i.ProductId > 0 && i.Quantity > 0);
            bool hasPurchaseRequest = !string.IsNullOrWhiteSpace(otherProductName) && parsedQuantity.HasValue && parsedQuantity > 0;

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
                purchaseRequestText = $"{otherProductName} (Qty: {parsedQuantity})";
                if (parsedPrice.HasValue && parsedPrice.Value > 0)
                    purchaseRequestText += $" Price: {parsedPrice.Value:C}";
                purchaseRequestText += " - Requested from supplier.";
                order.PurchaseRequestNotes = purchaseRequestText;
            }

            order.TotalAmount = total;
            order.OrderDate = DateTime.UtcNow;
            order.Status = hasItems ? "Pending" : "Purchase Request";
            order.CreatedAt = DateTime.UtcNow;

            try
            {
                if (validItems.Any())
                    await _orderRepo.AddOrderWithItemsAsync(order, validItems, purchaseRequestText);
                else
                    await _orderRepo.AddAsync(order);

                await _auditService.LogAsync("Order Created", "Order", order.Id, $"Customer: {order.CustomerId}, Total: {order.TotalAmount}, Status: {order.Status}");

                Console.WriteLine($"✅ Order created successfully. ID: {order.Id}");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR saving order: {ex.Message}");
                Console.WriteLine($"❌ Inner exception: {ex.InnerException?.Message}");
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

            var hoursSinceCreation = (DateTime.UtcNow - order.CreatedAt).TotalHours;
            if (hoursSinceCreation > 1 || order.Status == "Approved")
            {
                TempData["ErrorMessage"] = "This order cannot be edited. It is either approved or older than 1 hour.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(order);
        }

        // POST: /Order/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order order)
        {
            if (id != order.Id) return NotFound();

            var existing = await _orderRepo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            if (existing.Status == "Approved")
            {
                TempData["ErrorMessage"] = "Approved orders cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (ModelState.IsValid)
            {
                existing.Status = order.Status;
                existing.Notes = order.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
                await _orderRepo.UpdateAsync(existing);
                await _auditService.LogAsync("Order Updated", "Order", order.Id, $"Status: {order.Status}");
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(order);
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

            // Deduct stock
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
                    await _productRepo.UpdateAsync(product, $"Order #{order.Id} approved - {item.Quantity} units removed");
                }
            }

            order.Status = "Approved";
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepo.UpdateAsync(order);

            await _auditService.LogAsync("Order Approved", "Order", order.Id, $"Order #{order.Id} approved by {User.Identity?.Name ?? "System"}");

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Order/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();

            if (order.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending orders can be rejected.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = "Rejected";
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepo.UpdateAsync(order);

            await _auditService.LogAsync("Order Rejected", "Order", order.Id, $"Order #{order.Id} rejected by {User.Identity?.Name ?? "System"}");

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Order/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (order.Status != "Pending")
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
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (order.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending orders can be deleted.";
                return RedirectToAction(nameof(Index));
            }

            await _orderRepo.DeleteAsync(id);
            await _auditService.LogAsync("Order Deleted", "Order", id, $"Order #{id} was deleted (status: {order.Status})");

            return RedirectToAction(nameof(Index));
        }

        // GET: /Order/DownloadInvoice/5
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (order.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Invoice is only available for approved orders.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var invoice = $"Invoice for Order #{order.Id}\n";
            invoice += $"Customer: {order.CustomerId}\n";
            invoice += $"Date: {order.OrderDate:yyyy-MM-dd HH:mm}\n";
            invoice += "Items:\n";
            foreach (var item in order.OrderItems)
            {
                invoice += $"- {item.Product?.Name} x {item.Quantity} @ {item.Price:C} = {item.Price * item.Quantity:C}\n";
            }
            invoice += $"Total: {order.TotalAmount:C}\n";

            var bytes = System.Text.Encoding.UTF8.GetBytes(invoice);
            return File(bytes, "text/plain", $"Invoice_{order.Id}.txt");
        }

        // GET: /Order/ExportCsv
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
    }
}