using ApiX.Data.EFCore.Migrations;
using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Tests.TestHelpers.Seeders;

public sealed class CountriesSeeder : AbstractSeeder<TestDbContext>
{
    public override string Id => "Seed.Countries";
    public override int Order => 20;

    public override async Task<bool> ShouldRunAsync(TestDbContext db, IServiceProvider sp, CancellationToken ct = default) =>
        !await db.Countries.AnyAsync(ct);

    public override async Task RunAsync(TestDbContext db, IServiceProvider sp, CancellationToken ct = default)
    {
        db.AddRange(new Country(1, "South Africa"), new Country(2, "United States"));
        await Task.CompletedTask;
    }
}
