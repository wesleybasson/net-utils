using ApiX.Abstractions.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Tests.TestHelpers;

public sealed class TestEntity : IIdentifiableEntity
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public sealed class TestDbContext : DbContext
{
    public DbSet<TestEntity> Tests => Set<TestEntity>();
    public DbSet<Title> Titles => Set<Title>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<AppUser> Users => Set<AppUser>();

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Title>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).IsRequired(); });
        modelBuilder.Entity<Country>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).IsRequired(); });
        modelBuilder.Entity<AppUser>(e => { e.HasKey(x => x.Id); e.Property(x => x.Email).IsRequired(); });
    }
}

public record Title(int Id, string Name);
public record Country(int Id, string Name);
public record AppUser(int Id, string Email);
