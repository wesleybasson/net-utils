using ApiX.Abstractions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ApiX.Data.EFCore.Extensions;

/// <summary>
/// EF Core helpers for conditional inserts based on an entity's <c>Id</c>.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Adds <paramref name="entity"/> if an entity with <paramref name="id"/> does not exist.
    /// Returns <c>true</c> if the entity was added, otherwise <c>false</c>.
    /// </summary>
    public static async Task<bool> AddIfNotExistsAsync<TEntity>(
        this DbContext context,
        TEntity entity,
        Guid id,
        bool saveChanges = true,
        CancellationToken ct = default)
        where TEntity : class, IIdentifiableEntity
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entity);

        var set = context.Set<TEntity>();

        var exists = await set
            .AsNoTracking()
            .AnyAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);

        if (exists) return false;

        await set.AddAsync(entity, ct).ConfigureAwait(false);

        if (saveChanges)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Adds <paramref name="entity"/> if an entity with <paramref name="entity.Id"/> does not exist.
    /// Returns <c>true</c> if added.
    /// </summary>
    public static Task<bool> AddIfNotExistsAsync<TEntity>(
        this DbContext context,
        TEntity entity,
        bool saveChanges = true,
        CancellationToken ct = default)
        where TEntity : class, IIdentifiableEntity
    {
        ArgumentNullException.ThrowIfNull(entity);
        return context.AddIfNotExistsAsync(entity, entity.Id, saveChanges, ct);
    }

    /// <summary>
    /// Adds <paramref name="entity"/> if an entity with the parsed <paramref name="id"/> does not exist.
    /// Returns <c>true</c> if added.
    /// </summary>
    public static Task<bool> AddIfNotExistsAsync<TEntity>(
        this DbContext context,
        TEntity entity,
        string id,
        bool saveChanges = true,
        CancellationToken ct = default)
        where TEntity : class, IIdentifiableEntity
    {
        if (!Guid.TryParse(id, out var parsed))
            throw new ArgumentException("Value must be a valid GUID.", nameof(id));

        return context.AddIfNotExistsAsync(entity, parsed, saveChanges, ct);
    }

    /// <summary>
    /// Adds each entity from <paramref name="entities"/> whose <c>Id</c> is not present yet.
    /// Returns the number of entities added. Use <paramref name="saveChanges"/> to control DB round-trips.
    /// </summary>
    public static async Task<int> AddRangeIfNotExistsAsync<TEntity>(
        this DbContext context,
        IEnumerable<TEntity> entities,
        bool saveChanges = true,
        CancellationToken ct = default)
        where TEntity : class, IIdentifiableEntity
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entities);

        var set = context.Set<TEntity>();

        // materialize once to avoid multiple enumeration and to access Ids
        var list = entities as IList<TEntity> ?? entities.ToList();
        if (list.Count == 0) return 0;

        var ids = list.Select(e => e.Id).ToArray();

        var existingIds = await set
            .AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var toAdd = list.Where(e => !existingIds.Contains(e.Id)).ToList();
        if (toAdd.Count == 0) return 0;

        await set.AddRangeAsync(toAdd, ct).ConfigureAwait(false);

        if (saveChanges)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return toAdd.Count;
    }

    /// <summary>
    /// DbSet convenience overload.
    /// </summary>
    public static Task<bool> AddIfNotExistsAsync<TEntity>(
        this DbSet<TEntity> set,
        TEntity entity,
        Guid id,
        bool saveChanges = true,
        CancellationToken ct = default)
        where TEntity : class, IIdentifiableEntity
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(entity);

        var context = set.GetService<ICurrentDbContext>()?.Context
            ?? throw new InvalidOperationException("Could not resolve the owning DbContext from DbSet.");

        return context.AddIfNotExistsAsync(entity, id, saveChanges, ct);
    }
}
