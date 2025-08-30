using ApiX.Data.EFCore.Migrations;

namespace ApiX.Data.EFCore.Tests.TestHelpers.Seeders;

public sealed class FaultySeeder : AbstractSeeder<TestDbContext>
{
    public override string Id => "Seed.Faulty";
    public override int Order => 15;

    public override Task RunAsync(TestDbContext db, IServiceProvider sp, CancellationToken ct = default)
        => throw new InvalidOperationException("Boom");
}
