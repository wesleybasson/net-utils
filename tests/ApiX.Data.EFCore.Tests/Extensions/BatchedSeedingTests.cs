using ApiX.Data.EFCore.Extensions;
using ApiX.Data.EFCore.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Tests.Extensions;

public class BatchedSeedingTests : IClassFixture<SqliteInMemoryFixture>
{
    private readonly DbContextOptions<TestDbContext> _options;

    public BatchedSeedingTests(SqliteInMemoryFixture fx) => _options = fx.Options;

    private static TestEntity Make(Guid? id = null, string? name = null) =>
        new() { Id = id ?? Guid.NewGuid(), Name = name ?? "seed" };

    private void ResetDatabase()
    {
        using var ctx = new TestDbContext(_options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();
    }

    [Fact]
    public async Task Batched_SingleEntityCalls_SaveOnce()
    {
        ResetDatabase();

        await using var ctx = new TestDbContext(_options);

        var a = Make(name: "a");
        var b = Make(name: "b");
        var c = Make(name: "c");

        // All conditional adds without flushing:
        var addedA = await ctx.AddIfNotExistsAsync(a, saveChanges: false);
        var addedB = await ctx.AddIfNotExistsAsync(b, saveChanges: false);
        var addedC = await ctx.AddIfNotExistsAsync(c, saveChanges: false);

        Assert.True(addedA);
        Assert.True(addedB);
        Assert.True(addedC);

        // Nothing persisted yet:
        Assert.Equal(0, await ctx.Tests.CountAsync());

        // Single flush:
        await ctx.SaveChangesAsync();

        // Everything persisted:
        var names = await ctx.Tests.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name!).ToListAsync();
        Assert.Equal(new[] { "a", "b", "c" }, names);
    }

    [Fact]
    public async Task Batched_RangeHelper_SkipsDuplicates_SaveOnce()
    {
        ResetDatabase();

        await using var ctx = new TestDbContext(_options);

        // Seed one record that will appear as a duplicate in the batch
        var existingId = Guid.NewGuid();
        ctx.Tests.Add(Make(existingId, "existing"));
        await ctx.SaveChangesAsync();

        // Prepare a batch with two new + one duplicate
        var new1 = Make(name: "n1");
        var new2 = Make(name: "n2");
        var dup = Make(existingId, "dup"); // should be skipped

        // Range helper without flushing
        var addedCount = await ctx.AddRangeIfNotExistsAsync(new[] { new1, new2, dup }, saveChanges: false);
        Assert.Equal(2, addedCount);

        // Still only the original row is persisted so far
        Assert.Equal(1, await ctx.Tests.CountAsync());

        // Single flush for the batch
        await ctx.SaveChangesAsync();

        // Now we should have three rows total: existing + n1 + n2
        var names = await ctx.Tests.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name!).ToListAsync();
        Assert.Equal(new[] { "existing", "n1", "n2" }, names);
    }
}
