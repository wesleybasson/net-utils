using ApiX.Data.EFCore.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ApiX.Data.EFCore.Tests.Migrations;

public class MigrationOrchestratorTests
{
    [Fact]
    public async Task Runs_Seeders_In_Order_And_Persists_Data()
    {
        var (sp, conn) = TestHost.Build(opts =>
        {
            opts.AutoMigrate = true;
            opts.AutoSeed = true;
            opts.HaltSeeding = false;
            opts.AllowedEnvironments = new[] { "Development", "Production" };
        });

        // Ensure schema exists once (no actual migrations in tests)
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var orch = sp.GetRequiredService<TestOrchestrator>();
        await orch.ExecuteAsync();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            (await db.Titles.CountAsync()).Should().Be(3);
            (await db.Countries.CountAsync()).Should().Be(2);
        }

        orch.Events.Should().ContainInOrder(
            "BeforeMigrate:Development",
            "AfterMigrate",
            "Before:Seed.Titles",
            "After:Seed.Titles",
            "Before:Seed.Countries",
            "After:Seed.Countries"
        );

        await conn.DisposeAsync();
    }

    [Fact]
    public async Task Honors_HaltSeeding_Flag()
    {
        var (sp, conn) = TestHost.Build(opts =>
        {
            opts.AutoMigrate = true;
            opts.AutoSeed = true;
            opts.HaltSeeding = true;
        });

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var orch = sp.GetRequiredService<TestOrchestrator>();
        await orch.ExecuteAsync();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            (await db.Titles.AnyAsync()).Should().BeFalse();
            (await db.Countries.AnyAsync()).Should().BeFalse();
        }

        await conn.DisposeAsync();
    }

    [Fact]
    public async Task Environment_Filter_Allows_And_Blocks_As_Configured()
    {
        // Not allowed env
        var (sp1, conn1) = TestHost.Build(opts =>
        {
            opts.AutoSeed = true;
            opts.AllowedEnvironments = new[] { "Production" };
        }, environment: "Development");

        using (var s1 = sp1.CreateScope())
        {
            var db = s1.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }
        var orch1 = sp1.GetRequiredService<TestOrchestrator>();
        await orch1.ExecuteAsync();
        using (var s1 = sp1.CreateScope())
        {
            var db = s1.ServiceProvider.GetRequiredService<TestDbContext>();
            (await db.Titles.AnyAsync()).Should().BeFalse();
        }
        await conn1.DisposeAsync();

        // Allowed env
        var (sp2, conn2) = TestHost.Build(opts =>
        {
            opts.AutoSeed = true;
            opts.AllowedEnvironments = new[] { "Production" };
        }, environment: "Production");

        using (var s2 = sp2.CreateScope())
        {
            var db = s2.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }
        var orch2 = sp2.GetRequiredService<TestOrchestrator>();
        await orch2.ExecuteAsync();
        using (var s2 = sp2.CreateScope())
        {
            var db = s2.ServiceProvider.GetRequiredService<TestDbContext>();
            (await db.Titles.AnyAsync()).Should().BeTrue();
        }
        await conn2.DisposeAsync();
    }

    [Fact]
    public async Task ShouldRun_Gate_Prevents_Duplicate_Seeding()
    {
        var (sp, conn) = TestHost.Build(opts =>
        {
            opts.AutoSeed = true;
        });

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var orch = sp.GetRequiredService<TestOrchestrator>();
        await orch.ExecuteAsync(); // first run seeds
        await orch.ExecuteAsync(); // second run should skip due to ShouldRun

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            (await db.Titles.CountAsync()).Should().Be(3); // not 6
            (await db.Countries.CountAsync()).Should().Be(2);
        }

        orch.Events.Count(e => e.StartsWith("Skipped:Seed.Titles")).Should().BeGreaterThanOrEqualTo(1);
        orch.Events.Count(e => e.StartsWith("Skipped:Seed.Countries")).Should().BeGreaterThanOrEqualTo(1);

        await conn.DisposeAsync();
    }

    [Fact]
    public async Task Seeder_Exception_Can_Be_Swallowed_By_Hook_And_Pipeline_Continues()
    {
        var (sp, conn) = TestHost.Build(
            opts => { opts.AutoSeed = true; },
            registerFaultySeeder: true);

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var orch = sp.GetRequiredService<TestOrchestrator>();
        orch.Swallow = true; // let hook swallow exceptions
        await orch.ExecuteAsync();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            // Even though FaultySeeder failed, later seeders still ran
            (await db.Countries.CountAsync()).Should().Be(2);
            (await db.Titles.CountAsync()).Should().Be(3);
        }

        orch.Events.Should().Contain(e => e.StartsWith("Error:Seed.Faulty:InvalidOperationException"));
        await conn.DisposeAsync();
    }
}
