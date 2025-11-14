using Microsoft.EntityFrameworkCore;
using EfEx.SoftDelete.Interceptors;
using Xunit;

namespace EfEx.SoftDelete.Tests;

public class SoftDeleteInterceptorTests
{
    [Fact]
    public void SavingChanges_ConvertsHardDeleteToSoftDelete()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntity { Id = 1, Name = "Test", IsDeleted = false };
        context.TestEntities.Add(entity);
        context.SaveChanges();

        // Act - Delete the entity and save
        context.TestEntities.Remove(entity);
        context.SaveChanges();

        // Assert - Entity should be soft deleted, not hard deleted
        Assert.Equal(EntityState.Unchanged, context.Entry(entity).State);
        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedAt);
        Assert.InRange(entity.DeletedAt!.Value, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task SavingChangesAsync_ConvertsHardDeleteToSoftDelete()
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

        // Act - Delete the entity and save
        context.TestEntities.Remove(entity);
        await context.SaveChangesAsync();

        // Assert - Entity should be soft deleted, not hard deleted
        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedAt);
        Assert.InRange(entity.DeletedAt!.Value, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void SavingChanges_DoesNotAffectNonSoftDeletableEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new RegularEntity { Id = 1, Name = "Test" };
        context.RegularEntities.Add(entity);
        context.SaveChanges();

        // Act - Delete the entity and save
        context.RegularEntities.Remove(entity);
        context.SaveChanges();

        // Assert - Regular entities should be hard deleted
        var found = context.RegularEntities.Find(1);
        Assert.Null(found);
    }

    [Fact]
    public void SavingChanges_OnlyConvertsDeletedState()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntity { Id = 1, Name = "Test", IsDeleted = false };
        context.TestEntities.Add(entity);
        context.SaveChanges();

        // Act - Modify the entity (not delete)
        entity.Name = "Modified";
        context.SaveChanges();

        // Assert - Should not be soft deleted
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
    }

    [Fact]
    public void SavingChanges_SetsDeletedAtToUtcNow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntity { Id = 1, Name = "Test", IsDeleted = false };
        context.TestEntities.Add(entity);
        context.SaveChanges();

        var beforeDelete = DateTime.UtcNow;

        // Act
        context.TestEntities.Remove(entity);
        context.SaveChanges();

        var afterDelete = DateTime.UtcNow;

        // Assert
        Assert.NotNull(entity.DeletedAt);
        Assert.InRange(entity.DeletedAt!.Value, beforeDelete.AddSeconds(-1), afterDelete.AddSeconds(1));
    }

    [Fact]
    public void SavingChanges_EntityStillExistsInDatabaseAfterSoftDelete()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntity { Id = 1, Name = "Test", IsDeleted = false };
        context.TestEntities.Add(entity);
        context.SaveChanges();

        // Act - Delete the entity
        context.TestEntities.Remove(entity);
        context.SaveChanges();

        // Assert - Entity should still exist in database with IsDeleted = true
        var found = context.TestEntities.Find(1);
        Assert.NotNull(found);
        Assert.True(found!.IsDeleted);
    }
}

