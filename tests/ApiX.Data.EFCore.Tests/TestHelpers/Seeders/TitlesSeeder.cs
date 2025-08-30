using ApiX.Data.EFCore.Migrations;
using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Tests.TestHelpers.Seeders;

public sealed class TitlesSeeder : AbstractSeeder<TestDbContext>
{
    public override string Id => "Seed.Titles";
    public override int Order => 10;

    public override async Task<bool> ShouldRunAsync(TestDbContext db, IServiceProvider sp, CancellationToken ct = default) =>
        !await db.Titles.AnyAsync(ct);

    public override async Task RunAsync(TestDbContext db, IServiceProvider sp, CancellationToken ct = default)
    {
        db.AddRange(new Title(1, "Mr"), new Title(2, "Ms"), new Title(3, "Dr"));
        await Task.CompletedTask;
    }
}
