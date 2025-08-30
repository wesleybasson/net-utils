using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Migrations;

/// <summary>
/// Defines the contract for a single, idempotent database seeding unit.
/// </summary>
/// <typeparam name="TContext">The EF Core <see cref="DbContext"/> type being seeded.</typeparam>
public interface ISeeder<TContext> where TContext : DbContext
{
    /// <summary>
    /// Gets a stable identifier for this seeder, primarily used for logging and auditing.
    /// Recommended format: <c>"Seed.[Name].v[Version]"</c> (e.g., <c>"Seed.Countries.v1"</c>).
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the execution order across multiple seeders. 
    /// Seeders with lower values are executed first.
    /// Defaults to <c>0</c>.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Determines whether this seeder should run.
    /// Override to implement conditional checks (e.g., only run if certain data is missing).
    /// </summary>
    /// <param name="db">The current database context instance.</param>
    /// <param name="services">The application's service provider for resolving dependencies.</param>
    /// <param name="ct">A cancellation token that may be used to cancel the operation.</param>
    /// <returns>
    /// A task resolving to <c>true</c> if the seeder should run; otherwise <c>false</c>.
    /// </returns>
    Task<bool> ShouldRunAsync(TContext db, IServiceProvider services, CancellationToken ct = default);

    /// <summary>
    /// Executes the seeding logic.
    /// Implementations must be idempotent (safe to run multiple times without side effects).
    /// </summary>
    /// <param name="db">The current database context instance.</param>
    /// <param name="services">The application's service provider for resolving dependencies.</param>
    /// <param name="ct">A cancellation token that may be used to cancel the operation.</param>
    Task RunAsync(TContext db, IServiceProvider services, CancellationToken ct = default);
}

/// <summary>
/// Provides a base implementation of <see cref="ISeeder{TContext}"/> 
/// with sensible defaults for common scenarios.
/// </summary>
/// <typeparam name="TContext">The EF Core <see cref="DbContext"/> type being seeded.</typeparam>
public abstract class AbstractSeeder<TContext> : ISeeder<TContext> where TContext : DbContext
{
    /// <summary>
    /// Gets a stable identifier for this seeder.
    /// Must be overridden in derived classes.
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// Gets the execution order for this seeder.
    /// Defaults to <c>0</c> if not overridden.
    /// </summary>
    public virtual int Order => 0;

    /// <summary>
    /// Determines whether this seeder should run. 
    /// The default implementation always returns <c>true</c>.
    /// Override in derived classes for conditional execution.
    /// </summary>
    /// <param name="db">The current database context instance.</param>
    /// <param name="services">The application's service provider for resolving dependencies.</param>
    /// <param name="ct">A cancellation token that may be used to cancel the operation.</param>
    /// <returns>
    /// A task resolving to <c>true</c> to indicate that the seeder should run.
    /// </returns>
    public virtual Task<bool> ShouldRunAsync(TContext db, IServiceProvider services, CancellationToken ct = default)
        => Task.FromResult(true);

    /// <summary>
    /// Executes the seeding logic. 
    /// Must be overridden in derived classes and must be idempotent.
    /// </summary>
    /// <param name="db">The current database context instance.</param>
    /// <param name="services">The application's service provider for resolving dependencies.</param>
    /// <param name="ct">A cancellation token that may be used to cancel the operation.</param>
    public abstract Task RunAsync(TContext db, IServiceProvider services, CancellationToken ct = default);
}
