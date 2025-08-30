using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Migrations;

/// <summary>
/// Configuration options controlling database migrations and seeding behavior.
/// Typically bound from configuration (e.g., <c>appsettings.json</c>) or environment variables.
/// </summary>
public sealed class DatabaseMigrationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether EF Core migrations 
    /// DbContext.Database.MigrateAsync() should automatically run 
    /// before any seeding is attempted.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether seeders should automatically run 
    /// after migrations are applied, provided that seeding is not explicitly disabled.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool AutoSeed { get; set; } = true;

    /// <summary>
    /// Gets or sets a global kill switch that prevents all seeders from running, 
    /// regardless of configuration or environment. 
    /// Useful for disabling seeding entirely in production or automated pipelines.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool HaltSeeding { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of allowed environment names (values of 
    /// <c>ASPNETCORE_ENVIRONMENT</c>) in which seeding is permitted.
    /// The check is case-insensitive. 
    /// If empty, seeding is allowed in all environments.
    /// </summary>
    public string[] AllowedEnvironments { get; set; } = [];
}
