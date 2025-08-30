using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Tests.TestHelpers;

public sealed class SqliteInMemoryFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public DbContextOptions<TestDbContext> Options { get; }

    public SqliteInMemoryFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .Options;

        using var ctx = new TestDbContext(Options);
        ctx.Database.EnsureCreated(); // simple schema init
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
