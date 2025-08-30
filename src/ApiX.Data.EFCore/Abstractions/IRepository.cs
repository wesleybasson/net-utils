using ApiX.Abstractions.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ApiX.Data.EFCore.Abstractions;

/// <summary>Full repository (read + write).</summary>
public interface IRepository<T> : IReadRepository<T>
    where T : class, IIdentifiableEntity
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="saveChanges"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<EntityEntry<T>> CreateAsync(T entity, bool saveChanges = true, CancellationToken ct = default);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="saveChanges"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task CreateRangeAsync(IEnumerable<T> entities, bool saveChanges = true, CancellationToken ct = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="saveChanges"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<EntityEntry<T>> UpdateAsync(T entity, bool saveChanges = true, CancellationToken ct = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="saveChanges"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task DeleteAsync(T entity, bool saveChanges = true, CancellationToken ct = default);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="saveChanges"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task DeleteByIdAsync(Guid id, bool saveChanges = true, CancellationToken ct = default);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="saveChanges"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task DeleteManyAsync(IEnumerable<T> entities, bool saveChanges = true, CancellationToken ct = default);
}
