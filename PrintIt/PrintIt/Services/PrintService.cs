using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PrintIt.Data;
using PrintIt.Enums;
using PrintIt.Models;
using PrintIt.ViewModels;

namespace PrintIt.Services
{
    public class PrintService : IPrintService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _globalCacheDuration = TimeSpan.FromMinutes(30);

        public PrintService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<List<Print>> GetAllAsync()
        {
            return await _cache.GetOrCreateAsync("Prints:All", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _globalCacheDuration;
                return await _context.Prints.ToListAsync();
            });
        }

        public async Task<Print?> GetByIdAsync(Guid id)
        {
            return await _cache.GetOrCreateAsync($"Prints:{id}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _globalCacheDuration;
                return await _context.Prints.FindAsync(id);
            });
        }

        public async Task<List<Print>> GetByCategoryAsync(PrintType type)
        {
            return await _cache.GetOrCreateAsync($"Prints:Category:{type}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _globalCacheDuration;
                return await _context.Prints.Where(p => p.PrintType == type).ToListAsync();
            });
        }

        public async Task<List<Print>> SearchAsync(StoreSearchViewModel filter)
        {
            var query = _context.Prints.AsQueryable();

            if (!string.IsNullOrEmpty(filter.SearchString))
                query = query.Where(p => p.Name.Contains(filter.SearchString) || p.Description.Contains(filter.SearchString));

            if (filter.SelectedType.HasValue)
                query = query.Where(p => p.PrintType == filter.SelectedType.Value);

            if (filter.SelectedMaterial.HasValue)
                query = query.Where(p => p.MaterialType == filter.SelectedMaterial.Value);

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            return await query.OrderByDescending(p => p.AddedOn).ToListAsync();
        }

        public async Task AddAsync(Print print)
        {
            _context.Prints.Add(print);
            await _context.SaveChangesAsync();
            ClearCache();
        }

        public async Task UpdateAsync(Print print)
        {
            _context.Prints.Update(print);
            await _context.SaveChangesAsync();
            ClearCache();
        }

        public async Task DeleteAsync(Print print)
        {
            _context.Prints.Remove(print);
            await _context.SaveChangesAsync();
            ClearCache();
        }

        public void ClearCache()
        {
            _cache.Remove("Prints:All");
            foreach (var type in Enum.GetValues(typeof(PrintType)).Cast<PrintType>())
                _cache.Remove($"Prints:Category:{type}");
        }
    }
}