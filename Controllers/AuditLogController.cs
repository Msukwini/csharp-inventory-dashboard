using inventory_dashboard.Data;
using inventory_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace inventory_dashboard.Controllers
{
    public class AuditLogController : Controller
    {
        private readonly AppDbContext _context;

        public AuditLogController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
            return View(logs);
        }

        public async Task<IActionResult> ExportCsv()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,User,Action,EntityType,EntityId,Details");

            foreach (var l in logs)
            {
                csv.AppendLine($"{l.Timestamp:yyyy-MM-dd HH:mm},{l.User},{l.Action},{l.EntityType},{l.EntityId},{l.Details}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}