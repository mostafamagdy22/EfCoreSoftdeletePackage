# EfCore.SoftDelete

A lightweight Entity Framework Core extension that provides automatic soft delete functionality. This package automatically converts hard deletes to soft deletes and filters out deleted entities from queries.

## Features

- ✅ **Automatic Soft Delete Conversion**: Intercepts delete operations and converts them to soft deletes
- ✅ **Query Filtering**: Automatically excludes soft-deleted entities from queries
- ✅ **Simple Interface**: Just implement `ISoftDeletetable` on your entities
- ✅ **Async Support**: Works seamlessly with both synchronous and asynchronous operations
- ✅ **Zero Configuration**: Minimal setup required
- ✅ **EF Core 9.0 Compatible**: Built for the latest Entity Framework Core

## Installation

Install the package via NuGet:

```bash
dotnet add package EfCore.SoftDelete
```

Or via Package Manager Console:

```powershell
Install-Package EfCore.SoftDelete
```

## Quick Start

### 1. Implement the Interface

Make your entity implement `ISoftDeletetable`:

```csharp
using EfEx.SoftDelete.Models;

public class User : ISoftDeletetable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Soft delete properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### 2. Configure DbContext

Add the interceptor and enable soft delete in your `DbContext`:

```csharp
using Microsoft.EntityFrameworkCore;
using EfEx.SoftDelete.Interceptors;
using EfEx.SoftDelete.Extensions;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlServer("your-connection-string")
            .AddInterceptors(new SoftDeleteInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Enable automatic query filtering for soft-deleted entities
        modelBuilder.EnableSoftDelete();
    }
}
```

### 3. Use It!

Now when you delete an entity, it will be soft-deleted automatically:

```csharp
// This will soft-delete the user instead of hard-deleting
var user = await context.Users.FindAsync(1);
context.Users.Remove(user);
await context.SaveChangesAsync();

// The user still exists in the database with IsDeleted = true
// But it won't appear in normal queries
var activeUsers = await context.Users.ToListAsync(); // Won't include deleted users
```

## Usage Examples

### Basic Soft Delete

```csharp
// Delete an entity
var user = await context.Users.FindAsync(1);
context.Users.Remove(user);
await context.SaveChangesAsync();

// The entity is now soft-deleted:
// - IsDeleted = true
// - DeletedAt = DateTime.UtcNow
// - Still exists in database
```

### Querying Active Entities

Soft-deleted entities are automatically excluded from queries:

```csharp
// Only returns users where IsDeleted = false
var activeUsers = await context.Users.ToListAsync();

// FindAsync also respects the filter
var user = await context.Users.FindAsync(1); // Returns null if deleted
```

### Including Deleted Entities

To query soft-deleted entities, use `IgnoreQueryFilters()`:

```csharp
// Get all users including deleted ones
var allUsers = await context.Users
    .IgnoreQueryFilters()
    .ToListAsync();

// Find a specific user even if deleted
var deletedUser = await context.Users
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(u => u.Id == 1);
```

### Setting DeletedBy

You can set the `DeletedBy` property before deleting:

```csharp
var user = await context.Users.FindAsync(1);
user.DeletedBy = "admin@example.com";
context.Users.Remove(user);
await context.SaveChangesAsync();
```

### Restoring Soft-Deleted Entities

To restore a soft-deleted entity:

```csharp
var deletedUser = await context.Users
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(u => u.Id == 1 && u.IsDeleted);

if (deletedUser != null)
{
    deletedUser.IsDeleted = false;
    deletedUser.DeletedAt = null;
    deletedUser.DeletedBy = null;
    await context.SaveChangesAsync();
}
```

### Hard Delete (Permanent Delete)

If you need to permanently delete an entity, you can bypass the interceptor:

```csharp
var user = await context.Users
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(u => u.Id == 1);

if (user != null)
{
    // Manually set IsDeleted and save, then remove
    // Or use ExecuteDelete for EF Core 7+
    await context.Users
        .Where(u => u.Id == 1)
        .ExecuteDeleteAsync();
}
```

## API Reference

### ISoftDeletetable Interface

```csharp
public interface ISoftDeletetable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

### SoftDeleteInterceptor

Intercepts `SaveChanges` and `SaveChangesAsync` operations to convert hard deletes to soft deletes.

**Usage:**
```csharp
optionsBuilder.AddInterceptors(new SoftDeleteInterceptor());
```

### SoftDeleteExtensions

Extension methods for `ModelBuilder` to enable query filtering.

**Methods:**
- `EnableSoftDelete()`: Applies query filters to all entities implementing `ISoftDeletetable`

**Usage:**
```csharp
modelBuilder.EnableSoftDelete();
```

## Advanced Scenarios

### Multiple DbContexts

If you have multiple `DbContext` classes, add the interceptor and extension to each:

```csharp
public class UserDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new SoftDeleteInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.EnableSoftDelete();
    }
}
```

### Partial Soft Delete

You can have some entities with soft delete and others without:

```csharp
public class User : ISoftDeletetable
{
    // Has soft delete
}

public class LogEntry
{
    // No soft delete - will be hard deleted
}
```

### Custom DeletedBy Logic

You can set `DeletedBy` based on your authentication context:

```csharp
public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set DeletedBy before save
        var deletedEntries = ChangeTracker.Entries<ISoftDeletetable>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (var entry in deletedEntries)
        {
            entry.Entity.DeletedBy = _currentUser.UserId;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

## Migration

When adding soft delete to existing entities, create a migration:

```bash
dotnet ef migrations Add AddSoftDeleteToUsers
```

The migration will add the soft delete columns:

```csharp
migrationBuilder.AddColumn<bool>(
    name: "IsDeleted",
    table: "Users",
    nullable: false,
    defaultValue: false);

migrationBuilder.AddColumn<DateTime>(
    name: "DeletedAt",
    table: "Users",
    nullable: true);

migrationBuilder.AddColumn<string>(
    name: "DeletedBy",
    table: "Users",
    nullable: true);
```

## Requirements

- .NET 9.0 or later
- Entity Framework Core 9.0 or later

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/mostafamagdy22/EfCoreSoftdeletePackage).

## Related Packages

- [EfCore.Audit](https://www.nuget.org/packages/EfCore.Audit) - Entity Framework Core audit trail package

