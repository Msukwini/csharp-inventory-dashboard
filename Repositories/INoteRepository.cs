using inventory_dashboard.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace inventory_dashboard.Repositories
{
    public interface INoteRepository
    {
        Task<IEnumerable<Note>> GetAllAsync();
        Task<IEnumerable<Note>> GetByProductIdAsync(int productId);
        Task<Note?> GetByIdAsync(int id);
        Task AddAsync(Note note);
        Task UpdateAsync(Note note);
        Task DeleteAsync(int id);
    }
}