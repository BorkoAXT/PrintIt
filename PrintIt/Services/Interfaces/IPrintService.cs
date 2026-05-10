using PrintIt.Enums;
using PrintIt.Models;
using PrintIt.ViewModels;

namespace Services.Interfaces
{
    public interface IPrintService
    {
        Task<List<Print>> GetAllAsync();
        Task<Print?> GetByIdAsync(Guid id);
        Task<List<Print>> GetByCategoryAsync(PrintType type);
        Task<List<Print>> SearchAsync(StoreSearchViewModel filter);
        Task AddAsync(Print print);
        Task UpdateAsync(Print print);
        Task DeleteAsync(Print print);

        void ClearCache(Guid? id = null);
    }
}
