using ApiX.Abstractions.Domain;
using System.Linq.Expressions;

namespace ApiX.Data.EFCore.Abstractions;

/// <summary>
/// Query-only repository abstraction.
/// </summary>
public interface IReadRepository<T>
    where T : class, IIdentifiableEntity
{
    /// <summary>
    /// Returns a base queryable; set <paramref name="asTracking"/> to true when you intend to update.
    /// </summary>
    IQueryable<T> Query(bool asTracking = false);

    /// <summary>
    /// Get by primary key (Guid Id). Uses fast PK lookup when available.
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, bool asTracking = false, CancellationToken ct = default);

    /// <summary>
    /// Returns all items (beware of large sets).
    /// </summary>
    Task<List<T>> GetAllAsync(bool asTracking = false, CancellationToken ct = default);

    /// <summary>
    /// True if any entity matches the predicate.
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>
    /// Counts entities matching the predicate (or all if null).
    /// </summary>
    Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>
    /// First or default with an optional include shape.
    /// </summary>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asTracking = false,
        CancellationToken ct = default);
}
