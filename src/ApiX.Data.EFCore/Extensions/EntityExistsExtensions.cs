using ApiX.Abstractions.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Extensions;

/// <summary>
/// Convenience helpers to check for the existence of entities by their <see cref="IIdentifiableEntity.Id"/>.
/// Optimized for read-only lookups via <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/>.
/// </summary>
public static class EntityExistsExtensions
{
    /// <summary>
    /// Determines whether an entity with the specified <paramref name="id"/> exists.
    /// Executes a no-tracking query: <c>dbSet.AsNoTracking().Any(e =&gt; e.Id == id)</c>.
    /// </summary>
    /// <typeparam name="T">Entity type implementing <see cref="IIdentifiableEntity"/>.</typeparam>
    /// <param name="dbSet">The <see cref="DbSet{TEntity}"/> to query.</param>
    /// <param name="id">The entity identifier.</param>
    /// <returns><c>true</c> if an entity with <paramref name="id"/> exists; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dbSet"/> is <c>null</c>.</exception>
    public static bool ExistsById<T>(this DbSet<T> dbSet, Guid id)
        where T : class, IIdentifiableEntity
    {
        ArgumentNullException.ThrowIfNull(dbSet);
        return dbSet.AsNoTracking().Any(e => e.Id == id);
    }

    /// <summary>
    /// Asynchronously determines whether an entity with the specified <paramref name="id"/> exists.
    /// Executes a no-tracking query: <c>dbSet.AsNoTracking().AnyAsync(e =&gt; e.Id == id, ct)</c>.
    /// </summary>
    /// <typeparam name="T">Entity type implementing <see cref="IIdentifiableEntity"/>.</typeparam>
    /// <param name="dbSet">The <see cref="DbSet{TEntity}"/> to query.</param>
    /// <param name="id">The entity identifier.</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task producing <c>true</c> if an entity with <paramref name="id"/> exists; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dbSet"/> is <c>null</c>.</exception>
    public static Task<bool> ExistsByIdAsync<T>(this DbSet<T> dbSet, Guid id, CancellationToken ct = default)
        where T : class, IIdentifiableEntity
    {
        ArgumentNullException.ThrowIfNull(dbSet);
        return dbSet.AsNoTracking().AnyAsync(e => e.Id == id, ct);
    }

    /// <summary>
    /// Determines whether an entity with the specified string <paramref name="id"/> exists.
    /// Uses <see cref="Guid.TryParse(string, out Guid)"/>; returns <c>false</c> if parsing fails.
    /// </summary>
    /// <typeparam name="T">Entity type implementing <see cref="IIdentifiableEntity"/>.</typeparam>
    /// <param name="dbSet">The <see cref="DbSet{TEntity}"/> to query.</param>
    /// <param name="id">The entity identifier as a string (GUID).</param>
    /// <returns>
    /// <c>true</c> if parsing succeeds and an entity with the parsed identifier exists; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dbSet"/> is <c>null</c>.</exception>
    public static bool ExistsById<T>(this DbSet<T> dbSet, string id)
        where T : class, IIdentifiableEntity
    {
        ArgumentNullException.ThrowIfNull(dbSet);
        if (!Guid.TryParse(id, out var parsed)) return false;
        return dbSet.AsNoTracking().Any(e => e.Id == parsed);
    }

    /// <summary>
    /// Asynchronously determines whether an entity with the specified string <paramref name="id"/> exists.
    /// Uses <see cref="Guid.TryParse(string, out Guid)"/>; returns <c>false</c> if parsing fails.
    /// </summary>
    /// <typeparam name="T">Entity type implementing <see cref="IIdentifiableEntity"/>.</typeparam>
    /// <param name="dbSet">The <see cref="DbSet{TEntity}"/> to query.</param>
    /// <param name="id">The entity identifier as a string (GUID).</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task producing <c>true</c> if parsing succeeds and an entity with the parsed identifier exists; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dbSet"/> is <c>null</c>.</exception>
    public static Task<bool> ExistsByIdAsync<T>(this DbSet<T> dbSet, string id, CancellationToken ct = default)
        where T : class, IIdentifiableEntity
    {
        ArgumentNullException.ThrowIfNull(dbSet);
        if (!Guid.TryParse(id, out var parsed)) return Task.FromResult(false);
        return dbSet.AsNoTracking().AnyAsync(e => e.Id == parsed, ct);
    }
}
