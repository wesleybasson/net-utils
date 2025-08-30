using ApiX.Data.EFCore.Repositories;
using ApiX.Data.EFCore.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Tests.Repositories;

public class RepositoryTests : IClassFixture<SqliteInMemoryFixture>
{
    private readonly DbContextOptions<TestDbContext> _options;

    public RepositoryTests(SqliteInMemoryFixture fx)
    {
        _options = fx.Options;
        ResetDatabase();
    }

    private void ResetDatabase()
    {
        using var ctx = new TestDbContext(_options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();
    }

    private static TestEntity Make(Guid? id = null, string? name = null) =>
        new() { Id = id ?? Guid.NewGuid(), Name = name ?? "seed" };

    [Fact]
    public async Task CreateAsync_PersistsEntity()
    {
        await using var ctx = new TestDbContext(_options);
        var repo = new Repository<TestEntity>(ctx);

        var e = Make(name: "one");
        await repo.CreateAsync(e);

        var rows = await ctx.Tests.AsNoTracking().ToListAsync();
        Assert.Single(rows);
        Assert.Equal("one", rows[0].Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity()
    {
        await using var ctx = new TestDbContext(_options);
        var repo = new Repository<TestEntity>(ctx);

        var e = Make(name: "alpha");
        await repo.CreateAsync(e);

        var found = await repo.GetByIdAsync(e.Id);
        Assert.NotNull(found);
        Assert.Equal("alpha", found!.Name);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesEntity()
    {
        await using var ctx = new TestDbContext(_options);
        var repo = new Repository<TestEntity>(ctx);

        var e = Make(name: "old");
        await repo.CreateAsync(e);

        e.Name = "new";
        await repo.UpdateAsync(e);

        var updated = await ctx.Tests.AsNoTracking().SingleAsync();
        Assert.Equal("new", updated.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        await using var ctx = new TestDbContext(_options);
        var repo = new Repository<TestEntity>(ctx);

        var e = Make(name: "gone");
        await repo.CreateAsync(e);

        await repo.DeleteAsync(e);

        Assert.Empty(await ctx.Tests.ToListAsync());
    }

    [Fact]
    public async Task DeleteByIdAsync_RemovesEntity()
    {
        await using var ctx = new TestDbContext(_options);
        var repo = new Repository<TestEntity>(ctx);

        var e = Make(name: "id-delete");
        await repo.CreateAsync(e);

        await repo.DeleteByIdAsync(e.Id);

        Assert.Empty(await ctx.Tests.ToListAsync());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        await using var ctx = new TestDbContext(_options);
        var repo = new Repository<TestEntity>(ctx);

        await repo.CreateAsync(Make(name: "a"));
        await repo.CreateAsync(Make(name: "b"));

        var all = await repo.GetAllAsync();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, x => x.Name == "a");
        Assert.Contains(all, x => x.Name == "b");
    }

    [Fact]
    public async Task Query_AsTracking_AllowsModification()
    {
        await using var ctx = new TestDbContext(_options);
        var repo = new Repository<TestEntity>(ctx);

        var e = Make(name: "track-me");
        await repo.CreateAsync(e);

        var tracked = repo.Query(asTracking: true).Single(x => x.Id == e.Id);
        tracked.Name = "modified";

        await ctx.SaveChangesAsync();

        var updated = await ctx.Tests.AsNoTracking().SingleAsync();
        Assert.Equal("modified", updated.Name);
    }
}
