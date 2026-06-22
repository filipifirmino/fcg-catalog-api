using System;

namespace FCG_CATALOG_API.Domain.Interfaces;

public interface IRepositoryBase<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid id);
    Task<T?>  AddAsync (T entity);
    Task UpdateAsync (T entity);
    Task DeleteAsync (T entity);
}
