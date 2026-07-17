using System.Threading.Tasks;

namespace inventory_dashboard.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityType, int? entityId, string details);
    }
}
