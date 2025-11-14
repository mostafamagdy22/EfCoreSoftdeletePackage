using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EfEx.SoftDelete.Interceptors;
using Xunit;

namespace EfEx.SoftDelete.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task Interceptor_WithDbContext_SavesSoftDeletedEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntity { Id = 1, Name = "Test", IsDeleted = false };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act - Delete the entity
        context.TestEntities.Remove(entity);
        await context.SaveChangesAsync();

        // Assert - Entity should be soft deleted, not hard deleted
        // Use IgnoreQueryFilters to find soft-deleted entities
        var savedEntity = await context.TestEntities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == 1);
        Assert.NotNull(savedEntity);
        Assert.True(savedEntity!.IsDeleted);
        Assert.NotNull(savedEntity.DeletedAt);
        
        // Verify that normal query excludes soft-deleted entities
        var activeEntities = await context.TestEntities.ToListAsync();
        Assert.Empty(activeEntities);
    }

    [Fact]
    public async Task Interceptor_WithDbContext_DoesNotHardDeleteSoftDeletableEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntity { Id = 1, Name = "Test", IsDeleted = false };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act - Delete the entity
        context.TestEntities.Remove(entity);
        await context.SaveChangesAsync();

        // Assert - Entity should still exist in database with IsDeleted = true
        // Use IgnoreQueryFilters to see soft-deleted entities
        var allEntities = await context.TestEntities.IgnoreQueryFilters().ToListAsync();
        Assert.Single(allEntities);
        Assert.True(allEntities[0].IsDeleted);
    }

    [Fact]
    public async Task Interceptor_WithDbContext_HardDeletesNonSoftDeletableEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new RegularEntity { Id = 1, Name = "Test" };
        context.RegularEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act - Delete the entity
        context.RegularEntities.Remove(entity);
        await context.SaveChangesAsync();

        // Assert - Entity should be hard deleted
        var savedEntity = await context.RegularEntities.FindAsync(1);
        Assert.Null(savedEntity);
    }

    [Fact]
    public async Task Interceptor_WithDbContext_HandlesMultipleSoftDeletableEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        using var context = new TestDbContext(options);

        var entity1 = new TestEntity { Id = 1, Name = "Test1", IsDeleted = false };
        var entity2 = new TestEntity { Id = 2, Name = "Test2", IsDeleted = false };
        var entity3 = new TestEntity { Id = 3, Name = "Test3", IsDeleted = false };

        context.TestEntities.AddRange(entity1, entity2, entity3);
        await context.SaveChangesAsync();

        // Act - Delete multiple entities
        context.TestEntities.Remove(entity1);
        context.TestEntities.Remove(entity2);
        await context.SaveChangesAsync();

        // Assert - Use IgnoreQueryFilters to see all entities including soft-deleted ones
        var allEntities = await context.TestEntities.IgnoreQueryFilters().ToListAsync();
        Assert.Equal(3, allEntities.Count);
        Assert.True(allEntities.First(e => e.Id == 1).IsDeleted);
        Assert.True(allEntities.First(e => e.Id == 2).IsDeleted);
        Assert.False(allEntities.First(e => e.Id == 3).IsDeleted);
        
        // Verify that normal query excludes soft-deleted entities
        var activeEntities = await context.TestEntities.ToListAsync();
        Assert.Single(activeEntities);
        Assert.Equal(3, activeEntities[0].Id);
    }

    [Fact]
    public async Task Interceptor_WithDbContext_PreservesDeletedByProperty()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntity 
        { 
            Id = 1, 
            Name = "Test", 
            IsDeleted = false,
            DeletedBy = "User123"
        };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act - Delete the entity
        context.TestEntities.Remove(entity);
        await context.SaveChangesAsync();

        // Assert - DeletedBy should be preserved
        // Use IgnoreQueryFilters to find soft-deleted entities
        var savedEntity = await context.TestEntities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == 1);
        Assert.NotNull(savedEntity);
        Assert.Equal("User123", savedEntity!.DeletedBy);
    }
}

