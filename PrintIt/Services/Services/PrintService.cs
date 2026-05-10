using Common.Enums;
using Data.Repositories;
using Entities.Models;
using Entities.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Services.Interfaces;

namespace Services.Services
{
    public class PrintService : IPrintService
    {
        private readonly IRepository<Print> _printRepository;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _globalCacheDuration = TimeSpan.FromMinutes(30);

        public PrintService(IRepository<Print> printRepository, IMemoryCache cache)
        {
            _printRepository = printRepository;
            _cache = cache;
        }

        public async Task<List<Print>> GetAllAsync()
        {
            return await _cache.GetOrCreateAsync("Prints:All", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _globalCacheDuration;
                return await _printRepository.All().ToListAsync();
            }) ?? new List<Print>();
        }

        public async Task<Print?> GetByIdAsync(Guid id)
        {
            return await _cache.GetOrCreateAsync($"Prints:{id}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _globalCacheDuration;
                return await _printRepository.GetAsync(id);
            });
        }

        public async Task<List<Print>> GetByCategoryAsync(PrintType type)
        {
            return await _cache.GetOrCreateAsync($"Prints:Category:{type}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _globalCacheDuration;
                return await _printRepository.All()
                    .Where(p => p.PrintType == type)
                    .ToListAsync();
            }) ?? new List<Print>();
        }

        public async Task<List<Print>> SearchAsync(StoreSearchViewModel filter)
        {
            var query = _printRepository.All();

            if (!string.IsNullOrEmpty(filter.SearchString))
            {
                query = query.Where(p => p.Name.Contains(filter.SearchString) || p.Description.Contains(filter.SearchString));
            }

            if (filter.SelectedType.HasValue)
            {
                query = query.Where(p => p.PrintType == filter.SelectedType.Value);
            }

            if (filter.SelectedMaterial.HasValue)
            {
                query = query.Where(p => p.MaterialType == filter.SelectedMaterial.Value);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            return await query.OrderByDescending(p => p.AddedOn).ToListAsync();
        }

        public async Task AddAsync(Print print)
        {
            await _printRepository.AddAsync(print);
            ClearCache();
        }

        public async Task UpdateAsync(Print print)
        {
            await _printRepository.UpdateAsync(print);
            ClearCache();
        }

        public async Task DeleteAsync(Print print)
        {
            var id = print.Id;
            await _printRepository.DeleteAsync(print);

            ClearCache(id);
        }

        public void ClearCache(Guid? id = null)
        {
            _cache.Remove("Prints:All");

            foreach (var type in Enum.GetValues(typeof(PrintType)).Cast<PrintType>())
            {
                _cache.Remove($"Prints:Category:{type}");
            }

            if (id.HasValue)
            {
                _cache.Remove($"Prints:{id.Value}");
            }
        }
    }
}