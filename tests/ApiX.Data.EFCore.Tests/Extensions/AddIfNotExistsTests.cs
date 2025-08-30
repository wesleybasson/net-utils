using ApiX.Data.EFCore.Extensions;
using ApiX.Data.EFCore.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Tests.Extensions;

public class AddIfNotExistsTests : IClassFixture<SqliteInMemoryFixture>
{
    private readonly DbContextOptions<TestDbContext> _options;

    public AddIfNotExistsTests(SqliteInMemoryFixture fx) => _options = fx.Options;

    private static TestEntity Make(Guid? id = null, string? name = null) =>
        new() { Id = id ?? Guid.NewGuid(), Name = name ?? "alpha" };

    private void ResetDatabase()
    {
        using var ctx = new TestDbContext(_options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddIfNotExists_ById_Adds_WhenMissing()
    {
        ResetDatabase();

        await using var ctx = new TestDbContext(_options);
        var e = Make(id: Guid.NewGuid(), name: "one");

        var added = await ctx.AddIfNotExistsAsync(e, e.Id);

        Assert.True(added);
        Assert.Equal(1, await ctx.Tests.CountAsync());
    }

    [Fact]
    public async Task AddIfNotExists_ById_NoOp_WhenExists()
    {
        ResetDatabase();

        await using var ctx = new TestDbContext(_options);
        var id = Guid.NewGuid();
        ctx.Tests.Add(Make(id, "seed"));
        await ctx.SaveChangesAsync();

        var added = await ctx.AddIfNotExistsAsync(Make(id, "dup"), id);

        Assert.False(added);
        var rows = await ctx.Tests.AsNoTracking().Where(t => t.Id == id).ToListAsync();
        Assert.Single(rows);
        Assert.Equal("seed", rows[0].Name);
    }

    [Fact]
    public async Task AddIfNotExists_ByEntityId_Works()
    {
        ResetDatabase();

        await using var ctx = new TestDbContext(_options);
        var e = Make(name: "beta");

        var added = await ctx.AddIfNotExistsAsync(e);

        Assert.True(added);
        var found = await ctx.Tests.SingleAsync(t => t.Id == e.Id);
        Assert.Equal("beta", found.Name);
    }

    [Fact]
    public async Task AddIfNotExists_ByStringGuid_Works()
    {
        ResetDatabase();

        await using var ctx = new TestDbContext(_options);
        var id = Guid.NewGuid();
        var e = Make(id, "gamma");

        var added = await ctx.AddIfNotExistsAsync(e, id.ToString());

        Assert.True(added);
        Assert.NotNull(await ctx.Tests.FindAsync(id));
    }

    [Fact]
    public async Task AddIfNotExists_SaveChangesFalse_DoesNotFlush()
    {
        ResetDatabase();

        await using var ctx = new TestDbContext(_options);
        var e = Make(name: "delta");

        var added = await ctx.AddIfNotExistsAsync(e, saveChanges: false);

        Assert.True(added);
        // Not saved yet:
        Assert.Equal(0, await ctx.Tests.CountAsync());
        // After save:
        await ctx.SaveChangesAsync();
        Assert.Equal(1, await ctx.Tests.CountAsync());
    }

    [Fact]
    public async Task AddRangeIfNotExists_AddsOnlyNew_OnSingleSave()
    {
        ResetDatabase();

        await using var ctx = new TestDbContext(_options);

        var existingId = Guid.NewGuid();
        ctx.Tests.Add(Make(existingId, "seed"));
        await ctx.SaveChangesAsync();

        var a = Make(Guid.NewGuid(), "a");
        var b = Make(Guid.NewGuid(), "b");
        var dup = Make(existingId, "dup-should-skip");

        var addedCount = await ctx.AddRangeIfNotExistsAsync(new[] { a, b, dup });

        Assert.Equal(2, addedCount);
        var all = await ctx.Tests.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, x => x.Name == "seed");
        Assert.Contains(all, x => x.Name == "a");
        Assert.Contains(all, x => x.Name == "b");
    }

    [Fact]
    public async Task AddIfNotExists_HonorsCancellationToken()
    {
        ResetDatabase();

        await using var ctx = new TestDbContext(_options);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await ctx.AddIfNotExistsAsync(new TestEntity { Id = Guid.NewGuid(), Name = "x" }, saveChanges: true, ct: cts.Token);
        });
    }

    [Fact]
    public async Task Idempotency_RepeatedCalls_DoNotDuplicate()
    {
        ResetDatabase();

        await using var ctx = new TestDbContext(_options);
        var id = Guid.NewGuid();
        var e1 = Make(id, "first");

        var first = await ctx.AddIfNotExistsAsync(e1);
        var second = await ctx.AddIfNotExistsAsync(Make(id, "second"));

        Assert.True(first);
        Assert.False(second);
        var row = await ctx.Tests.SingleAsync(x => x.Id == id);
        Assert.Equal("first", row.Name);
    }
}
