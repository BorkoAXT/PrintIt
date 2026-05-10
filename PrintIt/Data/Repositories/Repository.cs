using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _set;
        private readonly IProperty[] _keyProperties;

        public Repository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _set = _context.Set<T>();

            var entityType = _context.Model.FindEntityType(typeof(T))
                ?? throw new InvalidOperationException($"Entity type '{typeof(T).Name}' is not part of the model.");

            _keyProperties = entityType.FindPrimaryKey()?.Properties.ToArray()
                ?? throw new InvalidOperationException($"Entity type '{typeof(T).Name}' does not have a primary key.");
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
            var tracked = _set.Local.FirstOrDefault(existing => HasSameKey(existing, entity));

            if (tracked != null)
            {
                _set.Remove(tracked);
                await _context.SaveChangesAsync();
                return tracked;
            }

            _context.Entry(entity).State = EntityState.Deleted;
            await _context.SaveChangesAsync();

            return entity;
        }

        public virtual async Task<List<T>> DeleteBulkAsync(List<T> entities)
        {
            foreach (var entity in entities)
            {
                var tracked = _set.Local.FirstOrDefault(existing => HasSameKey(existing, entity));

                if (tracked != null)
                {
                    _set.Remove(tracked);
                }
                else
                {
                    _context.Entry(entity).State = EntityState.Deleted;
                }
            }

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

        private bool HasSameKey(T left, T right)
        {
            return _keyProperties.All(p =>
                Equals(p.PropertyInfo?.GetValue(left), p.PropertyInfo?.GetValue(right)));
        }
    }
}