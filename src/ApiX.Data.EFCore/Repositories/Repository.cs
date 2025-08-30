using ApiX.Abstractions.Domain;
using ApiX.Data.EFCore.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace ApiX.Data.EFCore.Repositories;

/// <summary>
/// Generic EF Core repository implementation for entities with a <see cref="Guid"/> primary key
/// exposed via <see cref="IIdentifiableEntity.Id"/>.
/// </summary>
/// <typeparam name="T">Entity type implementing <see cref="IIdentifiableEntity"/>.</typeparam>
/// <remarks>
/// Provides a lightweight abstraction over <see cref="DbSet{TEntity}"/> operations with
/// convenience methods for CRUD and query operations. All methods support cancellation tokens
/// and an optional <c>saveChanges</c> flag to enable batching in a unit-of-work style.
/// </remarks>
public sealed class Repository<T> : IRepository<T>
    where T : class, IIdentifiableEntity
{
    private readonly DbContext _context;
    private readonly DbSet<T> _set;

    /// <summary>
    /// Initializes a new <see cref="Repository{T}"/> bound to the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> that manages this entity set.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
    public Repository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _set = _context.Set<T>();
    }

    // -------------------------
    // Query (IReadRepository<T>)
    // -------------------------

    /// <inheritdoc />
    public IQueryable<T> Query(bool asTracking = false) =>
        asTracking ? _set : _set.AsNoTracking();

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync(Guid id, bool asTracking = false, CancellationToken ct = default)
    {
        // Prefer FindAsync for PK lookups: uses change tracker + compiled PK accessors
        var found = await _set.FindAsync(new object?[] { id }, ct).ConfigureAwait(false);
        if (found is null) return null;

        if (!asTracking)
        {
            // If FindAsync tracked it, detach to simulate no-tracking semantics
            var entry = _context.Entry(found);
            entry.State = EntityState.Detached;
        }

        return found;
    }

    /// <inheritdoc />
    public async Task<List<T>> GetAllAsync(bool asTracking = false, CancellationToken ct = default) =>
        asTracking
            ? await _set.ToListAsync(ct).ConfigureAwait(false)
            : await _set.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        _set.AsNoTracking().AnyAsync(predicate, ct);

    /// <inheritdoc />
    public Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate is null
            ? _set.LongCountAsync(ct)
            : _set.LongCountAsync(predicate, ct);

    /// <inheritdoc />
    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asTracking = false,
        CancellationToken ct = default)
    {
        IQueryable<T> q = _set;
        if (!asTracking) q = q.AsNoTracking();
        if (include is not null) q = include(q);
        return await q.FirstOrDefaultAsync(predicate, ct).ConfigureAwait(false);
    }

    // -------------------------
    // Commands (IRepository<T>)
    // -------------------------

    /// <inheritdoc />
    /// <remarks>
    /// Adds the given <paramref name="entity"/> to the underlying <see cref="DbSet{TEntity}"/>.
    /// If <paramref name="saveChanges"/> is <c>true</c>, immediately calls
    /// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.
    /// </remarks>
    public async Task<EntityEntry<T>> CreateAsync(T entity, bool saveChanges = true, CancellationToken ct = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        var entry = await _set.AddAsync(entity, ct).ConfigureAwait(false);
        if (saveChanges) await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Adds a collection of entities in one batch. Optionally flushes immediately or waits for an external unit of work.
    /// </remarks>
    public async Task CreateRangeAsync(IEnumerable<T> entities, bool saveChanges = true, CancellationToken ct = default)
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));
        await _set.AddRangeAsync(entities, ct).ConfigureAwait(false);
        if (saveChanges) await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Attaches the entity if detached, then marks it as <see cref="EntityState.Modified"/>.
    /// </remarks>
    public async Task<EntityEntry<T>> UpdateAsync(T entity, bool saveChanges = true, CancellationToken ct = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            _set.Attach(entity);
            entry = _context.Entry(entity);
        }
        entry.State = EntityState.Modified;

        if (saveChanges) await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Removes the given entity from the set. If <paramref name="saveChanges"/> is true, persists the change immediately.
    /// </remarks>
    public async Task DeleteAsync(T entity, bool saveChanges = true, CancellationToken ct = default)
    {
        if (entity is null) return;
        _set.Remove(entity);
        if (saveChanges) await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Looks up an entity by <paramref name="id"/> and removes it if found.
    /// </remarks>
    public async Task DeleteByIdAsync(Guid id, bool saveChanges = true, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, asTracking: true, ct).ConfigureAwait(false);
        if (existing is null) return;
        _set.Remove(existing);
        if (saveChanges) await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Removes multiple entities from the set in one operation. Safe to call with an empty collection.
    /// </remarks>
    public async Task DeleteManyAsync(IEnumerable<T> entities, bool saveChanges = true, CancellationToken ct = default)
    {
        if (entities is null) return;
        _set.RemoveRange(entities);
        if (saveChanges) await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
