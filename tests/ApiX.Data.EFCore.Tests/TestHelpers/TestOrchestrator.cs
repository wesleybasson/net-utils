using ApiX.Data.EFCore.Migrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiX.Data.EFCore.Tests.TestHelpers;

public sealed class TestOrchestrator : MigrationOrchestrator<TestDbContext>
{
    public readonly List<string> Events = new();

    public TestOrchestrator(IServiceProvider sp, IOptions<DatabaseMigrationOptions> opts, ILoggerFactory lf)
        : base(sp, opts, lf) { }

    protected override Task OnBeforeMigrateAsync(TestDbContext db, string env, CancellationToken ct)
    { Events.Add($"BeforeMigrate:{env}"); return Task.CompletedTask; }

    protected override Task OnAfterMigrateAsync(TestDbContext db, string env, CancellationToken ct)
    { Events.Add("AfterMigrate"); return Task.CompletedTask; }

    protected override Task OnBeforeSeederAsync(TestDbContext db, ISeeder<TestDbContext> seeder, string env, CancellationToken ct)
    { Events.Add($"Before:{seeder.Id}"); return Task.CompletedTask; }

    protected override Task OnAfterSeederAsync(TestDbContext db, ISeeder<TestDbContext> seeder, string env, CancellationToken ct)
    { Events.Add($"After:{seeder.Id}"); return Task.CompletedTask; }

    protected override Task OnSeederSkippedAsync(TestDbContext db, ISeeder<TestDbContext> seeder, string env, CancellationToken ct)
    { Events.Add($"Skipped:{seeder.Id}"); return Task.CompletedTask; }

    // Return true to swallow exceptions (used by a test)
    public bool Swallow { get; set; } = false;
    protected override Task<bool> OnSeederExceptionAsync(TestDbContext db, ISeeder<TestDbContext> seeder, Exception ex, string env, CancellationToken ct)
    { Events.Add($"Error:{seeder.Id}:{ex.GetType().Name}"); return Task.FromResult(Swallow); }
}
