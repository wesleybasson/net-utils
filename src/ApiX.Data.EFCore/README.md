# ApiX.Data.EFCore

Opinionated helpers for building robust EF Core–backed data layers.  
Includes migration orchestration, repositories, and a set of useful LINQ/EF Core extensions.

---

## ✨ Features
- ✅ Centralized migration & seeding pipeline
- ✅ Generic repository & unit-of-work abstractions
- ✅ Helpful extension methods for EF Core & LINQ
- ✅ Configuration-first, DI-friendly design

---

## 📦 Installation
Reference the **ApiX.Data.EFCore** project or NuGet package in your service.

```xml
<ItemGroup>
  <PackageReference Include="ApiX.Data.EFCore" Version="x.y.z" />
</ItemGroup>
```

---

## 🚀 Migrations & Seeding

✔ Centralized orchestrator  
✔ Configurable via `DatabaseMigrationOptions`  
✔ Ordered, idempotent seeders

### 1. Define a Seeder
```csharp
public sealed class CountrySeeder : AbstractSeeder<MyDbContext>
{
    public override string Id => "Seed.Countries.v1";
    public override int Order => 10;

    public override async Task RunAsync(MyDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (!db.Countries.Any())
        {
            db.Countries.AddRange(
                new Country { Name = "South Africa" },
                new Country { Name = "Germany" }
            );
        }
        await Task.CompletedTask;
    }
}
```

### 2. Register in DI
```csharp
builder.Services.AddDbContext<MyDbContext>(...);

builder.Services.AddMigrationsPipeline<MyDbContext, DefaultMigrationOrchestrator<MyDbContext>>(builder.Configuration)
                .AddSeeder<MyDbContext, CountrySeeder>();
```

### 3. Run Migrate + Seed
```csharp
var app = builder.Build();
await app.MigrateAndSeedAsync<MyDbContext, DefaultMigrationOrchestrator<MyDbContext>>();
```

### Configuration
```json
{
  "Database": {
    "AutoMigrate": true,
    "AutoSeed": true,
    "HaltSeeding": false,
    "AllowedEnvironments": [ "Development", "Staging" ]
  }
}
```

---

## 📚 Repository Pattern

### Abstractions
- **`IReadRepository<TEntity>`** – read-only access (query, get by ID, count, etc.)
- **`IRepository<TEntity>`** – full CRUD contract for aggregate roots/entities
- **`IUnitOfWork`** – transaction boundary abstraction (commit/rollback)

### Implementation
A default `Repository<TEntity>` implementation is provided for EF Core:

```csharp
public class UserService
{
    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _uow;

    public UserService(IRepository<User> users, IUnitOfWork uow)
    {
        _users = users;
        _uow = uow;
    }

    public async Task<Guid> RegisterUserAsync(User user, CancellationToken ct)
    {
        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        return user.Id;
    }
}
```

### Registration
```csharp
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<MyDbContext>>();
```

---

## 🧩 Extensions

### 1. `DbContextExtensions`
Utility methods for working with `DbContext` more fluently.

Example:  
```csharp
await dbContext.AddIfNotExistsAsync(entity, e => e.UniqueKey);
```

### 2. `EntityExistsExtensions`
Adds guard-style checks for verifying existence by ID or predicate.

Example:  
```csharp
if (!db.Users.EntityGuidExists(userId))
    throw new NotFoundException();
```

### 3. `QueryableExtensions`
Convenience methods for LINQ and EF queries, such as pagination.

Example:  
```csharp
var page = await db.Users.AsQueryable()
    .ToPagedResultAsync(pageIndex, pageSize, ct);
```

---

## 📚 Next Steps

- Extend documentation for **Configurations** and **Queries** namespaces
- Add usage samples for advanced EF Core scenarios (global filters, interceptors, etc.)

---
