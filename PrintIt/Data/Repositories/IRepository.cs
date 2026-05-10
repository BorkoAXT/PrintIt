namespace Data.Repositories
{
    public interface IRepository<T> : IDisposable where T : class
    {
        IQueryable<T> All();

        Task<T?> GetAsync(object id);

        Task<T> AddAsync(T entity);

        Task<List<T>> AddBulkAsync(List<T> entities);

        Task<T> UpdateAsync(T entity);

        Task<T> DeleteAsync(T entity);

        Task<List<T>> DeleteBulkAsync(List<T> entities);

        Task<int> SaveChangesAsync();
    }
}