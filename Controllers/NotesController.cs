using inventory_dashboard.Models;
using inventory_dashboard.Repositories;
using inventory_dashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace inventory_dashboard.Controllers
{
    public class NotesController : Controller
    {
        private readonly INoteRepository _noteRepo;
        private readonly IProductRepository _productRepo;
        private readonly IAuditService _auditService;

        public NotesController(INoteRepository noteRepo, IProductRepository productRepo, IAuditService auditService)
        {
            _noteRepo = noteRepo;
            _productRepo = productRepo;
            _auditService = auditService;
        }

        // GET: Notes
        public async Task<IActionResult> Index(string? category, bool? completed)
        {
            var notes = await _noteRepo.GetAllAsync();

            if (!string.IsNullOrEmpty(category))
                notes = notes.Where(n => n.Category == category);
            if (completed.HasValue)
                notes = notes.Where(n => n.IsCompleted == completed.Value);

            ViewBag.CurrentCategory = category;
            ViewBag.CurrentCompleted = completed;

            return View(notes);
        }

        // GET: Notes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var note = await _noteRepo.GetByIdAsync(id);
            if (note == null) return NotFound();
            return View(note);
        }

        // GET: Notes/Create
        public async Task<IActionResult> Create(int? productId)
        {
            ViewBag.ProductId = productId;
            var products = await _productRepo.GetAllAsync();
            ViewBag.ProductList = new SelectList(products, "Id", "Name");
            return View();
        }

        // POST: Notes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Note note)
        {
            note.CreatedAt = DateTime.UtcNow;
            note.CreatedBy = User.Identity?.Name ?? "System";

            if (ModelState.IsValid)
            {
                await _noteRepo.AddAsync(note);
                await _auditService.LogAsync("Note Created", "Note", note.Id, $"Title: {note.Title}, Category: {note.Category}");
                return RedirectToAction(nameof(Index));
            }

            var products = await _productRepo.GetAllAsync();
            ViewBag.ProductList = new SelectList(products, "Id", "Name");
            return View(note);
        }

        // GET: Notes/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var note = await _noteRepo.GetByIdAsync(id);
            if (note == null) return NotFound();

            var products = await _productRepo.GetAllAsync();
            ViewBag.ProductList = new SelectList(products, "Id", "Name", note.ProductId);
            return View(note);
        }

        // POST: Notes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Note note)
        {
            if (id != note.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _noteRepo.UpdateAsync(note);
                await _auditService.LogAsync("Note Updated", "Note", note.Id, $"Title: {note.Title}, Category: {note.Category}");
                return RedirectToAction(nameof(Index));
            }

            var products = await _productRepo.GetAllAsync();
            ViewBag.ProductList = new SelectList(products, "Id", "Name", note.ProductId);
            return View(note);
        }

        // GET: Notes/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var note = await _noteRepo.GetByIdAsync(id);
            if (note == null) return NotFound();
            return View(note);
        }

        // POST: Notes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var note = await _noteRepo.GetByIdAsync(id);
            if (note == null) return NotFound();

            // Log before soft delete so we capture the title
            await _auditService.LogAsync("Note Deleted", "Note", note.Id, $"Title: {note.Title}, Category: {note.Category}");

            // Soft delete
            await _noteRepo.DeleteAsync(id);

            return RedirectToAction(nameof(Index));
        }
    }
}