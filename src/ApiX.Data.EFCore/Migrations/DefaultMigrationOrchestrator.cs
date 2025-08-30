using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiX.Data.EFCore.Migrations;

/// <summary>
/// Default implementation of a <see cref="MigrationOrchestrator{TContext}"/> 
/// that applies migrations and executes seeders according to <see cref="DatabaseMigrationOptions"/>.
/// </summary>
/// <typeparam name="TContext">The EF Core <see cref="DbContext"/> type to be migrated and seeded.</typeparam>
public sealed class DefaultMigrationOrchestrator<TContext> : MigrationOrchestrator<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMigrationOrchestrator{TContext}"/> class.
    /// </summary>
    /// <param name="sp">
    /// The root <see cref="IServiceProvider"/> used to resolve scoped services, seeders, and the DbContext.
    /// </param>
    /// <param name="opts">
    /// An options wrapper containing <see cref="DatabaseMigrationOptions"/> that control migration and seeding behavior.
    /// </param>
    /// <param name="lf">
    /// The <see cref="ILoggerFactory"/> used to create loggers for tracking orchestration activity.
    /// </param>
    public DefaultMigrationOrchestrator(
        IServiceProvider sp,
        IOptions<DatabaseMigrationOptions> opts,
        ILoggerFactory lf)
        : base(sp, opts, lf) { }
}
