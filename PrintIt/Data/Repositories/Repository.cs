using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _set;

        public Repository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _set = _context.Set<T>();
        }

        public virtual IQueryable<T> All()
        {
            return _set.AsNoTracking();
        }

        public virtual async Task<T?> GetAsync(object id)
        {
            return await _set.FindAsync(id);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            _set.Add(entity);
            await _context.SaveChangesAsync();

            return entity;
        }

        public virtual async Task<List<T>> AddBulkAsync(List<T> entities)
        {
            _set.AddRange(entities);
            await _context.SaveChangesAsync();

            return entities;
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            var entry = _context.Entry(entity);
            entry.State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T> DeleteAsync(T entity)
        {
            var entry = _context.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                _set.Attach(entity);
            }

            _set.Remove(entity);
            await _context.SaveChangesAsync();

            return entity;
        }

        public virtual async Task<List<T>> DeleteBulkAsync(List<T> entities)
        {
            _set.RemoveRange(entities);
            await _context.SaveChangesAsync();

            return entities;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}