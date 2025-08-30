using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiX.Data.EFCore.Migrations;

/// <summary>
/// Extension methods for registering EF Core migration and seeding infrastructure
/// into the application's dependency injection (DI) container and pipeline.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Registers the EF Core <see cref="DbContext"/>, migration orchestrator, and seeding pipeline.
    /// Call this from <c>Program.cs</c> (or <c>Startup</c> in older templates) during service registration.
    /// </summary>
    /// <typeparam name="TContext">The EF Core <see cref="DbContext"/> to manage migrations for.</typeparam>
    /// <typeparam name="TOrchestrator">
    /// The concrete orchestrator type derived from <see cref="MigrationOrchestrator{TContext}"/>.
    /// </typeparam>
    /// <param name="services">The application’s service collection.</param>
    /// <param name="config">The configuration source (e.g., <c>appsettings.json</c>).</param>
    /// <param name="configure">
    /// Optional delegate to override or post-configure <see cref="DatabaseMigrationOptions"/> after binding.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMigrationsPipeline<TContext, TOrchestrator>(
        this IServiceCollection services,
        IConfiguration config,
        Action<DatabaseMigrationOptions>? configure = null)
        where TContext : DbContext
        where TOrchestrator : MigrationOrchestrator<TContext>
    {
        // Bind options from "Database" (default section)
        services.Configure<DatabaseMigrationOptions>(config.GetSection("Database"));
        if (configure is not null) services.PostConfigure(configure);

        // Register orchestrator
        services.AddSingleton<TOrchestrator>();

        return services;
    }

    /// <summary>
    /// Registers a single database seeder in the DI container. 
    /// Seeders are resolved and executed by the orchestrator in order of their <see cref="ISeeder{TContext}.Order"/>.
    /// </summary>
    /// <typeparam name="TContext">The EF Core <see cref="DbContext"/> that the seeder targets.</typeparam>
    /// <typeparam name="TSeeder">The concrete seeder implementation.</typeparam>
    /// <param name="services">The application’s service collection.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSeeder<TContext, TSeeder>(this IServiceCollection services)
        where TContext : DbContext
        where TSeeder : class, ISeeder<TContext>
    {
        services.AddTransient<ISeeder<TContext>, TSeeder>();
        return services;
    }

    /// <summary>
    /// Runs EF Core migrations and seeders via the registered orchestrator.
    /// Should be called once during application startup after building the <see cref="WebApplication"/>.
    /// </summary>
    /// <typeparam name="TContext">The EF Core <see cref="DbContext"/> to manage.</typeparam>
    /// <typeparam name="TOrchestrator">The orchestrator responsible for migrations and seeding.</typeparam>
    /// <param name="app">The application instance.</param>
    /// <param name="ct">A cancellation token to observe while awaiting completion.</param>
    /// <returns>The same <see cref="WebApplication"/> for chaining startup calls.</returns>
    public static async Task<WebApplication> MigrateAndSeedAsync<TContext, TOrchestrator>(
        this WebApplication app, CancellationToken ct = default)
        where TContext : DbContext
        where TOrchestrator : MigrationOrchestrator<TContext>
    {
        var orchestrator = app.Services.GetRequiredService<TOrchestrator>();
        await orchestrator.ExecuteAsync(ct);
        return app;
    }
}
