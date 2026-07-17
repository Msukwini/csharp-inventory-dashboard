using inventory_dashboard.Data;
using inventory_dashboard.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace inventory_dashboard.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string entityType, int? entityId, string details)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "System";
            var log = new AuditLog
            {
                User = username,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
