using ApiX.Data.EFCore.Extensions;
using ApiX.Data.EFCore.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Tests.Extensions;

public class EntityExistsExtensionsTests : IClassFixture<SqliteInMemoryFixture>
{
    private readonly DbContextOptions<TestDbContext> _options;

    public EntityExistsExtensionsTests(SqliteInMemoryFixture fx)
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
    public void ExistsById_Guid_ReturnsTrueIfPresent()
    {
        using var ctx = new TestDbContext(_options);
        var e = Make();
        ctx.Tests.Add(e);
        ctx.SaveChanges();

        Assert.True(ctx.Tests.ExistsById(e.Id));
        Assert.False(ctx.Tests.ExistsById(Guid.NewGuid()));
    }

    [Fact]
    public async Task ExistsByIdAsync_Guid_ReturnsTrueIfPresent()
    {
        await using var ctx = new TestDbContext(_options);
        var e = Make();
        ctx.Tests.Add(e);
        await ctx.SaveChangesAsync();

        Assert.True(await ctx.Tests.ExistsByIdAsync(e.Id));
        Assert.False(await ctx.Tests.ExistsByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public void ExistsById_String_ReturnsTrueIfPresent()
    {
        using var ctx = new TestDbContext(_options);
        var e = Make();
        ctx.Tests.Add(e);
        ctx.SaveChanges();

        Assert.True(ctx.Tests.ExistsById(e.Id.ToString()));
        Assert.False(ctx.Tests.ExistsById(Guid.NewGuid().ToString()));
        Assert.False(ctx.Tests.ExistsById("not-a-guid")); // invalid GUID path
    }

    [Fact]
    public async Task ExistsByIdAsync_String_ReturnsTrueIfPresent()
    {
        await using var ctx = new TestDbContext(_options);
        var e = Make();
        ctx.Tests.Add(e);
        await ctx.SaveChangesAsync();

        Assert.True(await ctx.Tests.ExistsByIdAsync(e.Id.ToString()));
        Assert.False(await ctx.Tests.ExistsByIdAsync(Guid.NewGuid().ToString()));
        Assert.False(await ctx.Tests.ExistsByIdAsync("bad-guid")); // invalid GUID path
    }
}
