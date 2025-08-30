using ApiX.Data.EFCore.Migrations;
using ApiX.Data.EFCore.Tests.TestHelpers.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace ApiX.Data.EFCore.Tests.TestHelpers;

public static class TestHost
{
    public static (ServiceProvider sp, DbConnection conn) Build(
        Action<DatabaseMigrationOptions>? configure = null,
        bool registerFaultySeeder = false,
        string? environment = "Development")
    {
        // Keep the connection open and share it for all DbContext instances/scopes
        var conn = new SqliteConnection("DataSource=:memory:;Cache=Shared");
        conn.Open();

        var services = new ServiceCollection();

        services.AddLogging(b => b.AddDebug().AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddDbContext<TestDbContext>(o => o.UseSqlite(conn));

        services.Configure<DatabaseMigrationOptions>(_ => { });
        if (configure != null) services.PostConfigure(configure);

        services.AddTransient<ISeeder<TestDbContext>, TitlesSeeder>();
        services.AddTransient<ISeeder<TestDbContext>, CountriesSeeder>();
        if (registerFaultySeeder)
            services.AddTransient<ISeeder<TestDbContext>, FaultySeeder>();

        services.AddSingleton<TestOrchestrator>();

        // Set environment variable for orchestrator filtering
        if (environment is not null)
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);

        return (services.BuildServiceProvider(), conn);
    }
}
