using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiX.Data.EFCore.Migrations;

/// <summary>
/// Coordinates EF Core migrations and the execution of seeders with extensibility points
/// (hooks) for logging, telemetry, and custom policies.
/// </summary>
/// <typeparam name="TContext">The EF Core <see cref="DbContext"/> type being migrated and seeded.</typeparam>
public abstract class MigrationOrchestrator<TContext> where TContext : DbContext
{
    /// <summary>
    /// The root service provider used to create scopes and resolve dependencies such as the <see cref="DbContext"/> and seeders.
    /// </summary>
    protected readonly IServiceProvider Services;

    /// <summary>
    /// Logger instance for diagnostic and audit output during migrations and seeding.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Migration and seeding configuration options.
    /// </summary>
    protected readonly DatabaseMigrationOptions Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationOrchestrator{TContext}"/> class.
    /// </summary>
    /// <param name="services">The root <see cref="IServiceProvider"/> used to resolve scoped services.</param>
    /// <param name="opts">Configuration options governing migration and seeding behavior.</param>
    /// <param name="loggerFactory">
    /// Optional logger factory. If omitted, one will be resolved from the service provider.
    /// </param>
    protected MigrationOrchestrator(
        IServiceProvider services,
        IOptions<DatabaseMigrationOptions> opts,
        ILoggerFactory? loggerFactory = null)
    {
        Services = services;
        Options = opts.Value;
        Logger = (loggerFactory ?? services.GetRequiredService<ILoggerFactory>())
                 .CreateLogger(GetType());
    }

    /// <summary>
    /// Main entry point that executes database migrations and seeders in sequence,
    /// honoring configured options and allowed environments.
    /// </summary>
    /// <param name="ct">A cancellation token to observe while awaiting completion.</param>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider;
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;

        await OnBeforeAllAsync(env, ct);

        // Resolve context
        var db = provider.GetRequiredService<TContext>();

        if (Options.AutoMigrate)
        {
            await OnBeforeMigrateAsync(db, env, ct);
            await db.Database.MigrateAsync(ct);
            await OnAfterMigrateAsync(db, env, ct);
        }

        if (Options.AutoSeed && !Options.HaltSeeding && IsEnvironmentAllowed(env))
        {
            await OnBeforeSeedingAsync(db, env, ct);

            var seeders = provider.GetServices<ISeeder<TContext>>()
                                  .OrderBy(s => s.Order)
                                  .ToList();

            foreach (var seeder in seeders)
            {
                try
                {
                    if (await seeder.ShouldRunAsync(db, provider, ct))
                    {
                        await OnBeforeSeederAsync(db, seeder, env, ct);
                        await seeder.RunAsync(db, provider, ct);
                        await db.SaveChangesAsync(ct);
                        await OnAfterSeederAsync(db, seeder, env, ct);
                    }
                    else
                    {
                        await OnSeederSkippedAsync(db, seeder, env, ct);
                    }
                }
                catch (Exception ex)
                {
                    var handled = await OnSeederExceptionAsync(db, seeder, ex, env, ct);
                    if (!handled) throw; // bubble if not handled
                }
            }

            await OnAfterSeedingAsync(db, env, ct);
        }

        await OnAfterAllAsync(env, ct);
    }

    /// <summary>
    /// Checks whether the current environment is allowed to run seeding operations.
    /// </summary>
    /// <param name="env">The current ASP.NET Core environment string.</param>
    /// <returns><c>true</c> if seeding is allowed in this environment; otherwise <c>false</c>.</returns>
    protected virtual bool IsEnvironmentAllowed(string env)
        => Options.AllowedEnvironments.Length == 0
           || Options.AllowedEnvironments.Any(a =>
                  string.Equals(a, env, StringComparison.InvariantCultureIgnoreCase));

    // ==== Overridable hooks (for logging, telemetry, and custom policies) ====

    /// <summary>
    /// Executed before any migration or seeding begins.
    /// </summary>
    protected virtual Task OnBeforeAllAsync(string environment, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Executed after all migration and seeding completes.
    /// </summary>
    protected virtual Task OnAfterAllAsync(string environment, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Executed before migrations are applied.
    /// </summary>
    protected virtual Task OnBeforeMigrateAsync(TContext db, string environment, CancellationToken ct)
    {
        Logger.LogInformation("Starting EF Core migration for {Context} in {Env}", typeof(TContext).Name, environment);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executed after migrations have been applied.
    /// </summary>
    protected virtual Task OnAfterMigrateAsync(TContext db, string environment, CancellationToken ct)
    {
        Logger.LogInformation("Completed EF Core migration for {Context}", typeof(TContext).Name);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executed before any seeders run.
    /// </summary>
    protected virtual Task OnBeforeSeedingAsync(TContext db, string environment, CancellationToken ct)
    {
        Logger.LogInformation("Starting seeding in {Env}", environment);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executed after all seeders complete.
    /// </summary>
    protected virtual Task OnAfterSeedingAsync(TContext db, string environment, CancellationToken ct)
    {
        Logger.LogInformation("Completed seeding.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executed before an individual seeder runs.
    /// </summary>
    protected virtual Task OnBeforeSeederAsync(TContext db, ISeeder<TContext> seeder, string environment, CancellationToken ct)
    {
        Logger.LogInformation("Running seeder {Id} (Order {Order})", seeder.Id, seeder.Order);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executed after an individual seeder successfully completes.
    /// </summary>
    protected virtual Task OnAfterSeederAsync(TContext db, ISeeder<TContext> seeder, string environment, CancellationToken ct)
    {
        Logger.LogInformation("Completed seeder {Id}", seeder.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executed when an individual seeder is skipped (based on <see cref="ISeeder{TContext}.ShouldRunAsync"/>).
    /// </summary>
    protected virtual Task OnSeederSkippedAsync(TContext db, ISeeder<TContext> seeder, string environment, CancellationToken ct)
    {
        Logger.LogInformation("Skipped seeder {Id}", seeder.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executed when an exception occurs in a seeder. 
    /// Return <c>true</c> if the exception has been handled (swallowed), or <c>false</c> to rethrow.
    /// </summary>
    protected virtual Task<bool> OnSeederExceptionAsync(TContext db, ISeeder<TContext> seeder, Exception ex, string environment, CancellationToken ct)
    {
        Logger.LogError(ex, "Seeder {Id} failed.", seeder.Id);
        return Task.FromResult(false);
    }
}
