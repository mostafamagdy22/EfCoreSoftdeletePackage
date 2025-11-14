using Microsoft.EntityFrameworkCore;
using EfEx.SoftDelete.Extensions;
using Xunit;

namespace EfEx.SoftDelete.Tests;

public class SoftDeleteExtensionsTests
{
    [Fact]
    public void EnableSoftDelete_AppliesQueryFilterToSoftDeletableEntities()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestEntity>();
        modelBuilder.Entity<RegularEntity>();

        // Act
        modelBuilder.EnableSoftDelete();

        // Assert - Query filter should be applied to TestEntity (ISoftDeletetable)
        // but not to RegularEntity
        var testEntityType = modelBuilder.Model.FindEntityType(typeof(TestEntity));
        var regularEntityType = modelBuilder.Model.FindEntityType(typeof(RegularEntity));

        Assert.NotNull(testEntityType);
        Assert.NotNull(regularEntityType);
        
        // The query filter should be set on the soft deletable entity
        var queryFilter = testEntityType!.GetQueryFilter();
        Assert.NotNull(queryFilter);
    }

    [Fact]
    public async Task QueryFilter_ExcludesSoftDeletedEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var activeEntity = new TestEntity { Id = 1, Name = "Active", IsDeleted = false };
        var deletedEntity = new TestEntity { Id = 2, Name = "Deleted", IsDeleted = true, DeletedAt = DateTime.UtcNow };

        context.TestEntities.AddRange(activeEntity, deletedEntity);
        await context.SaveChangesAsync();

        // Act - Query should automatically exclude deleted entities
        var activeOnly = await context.TestEntities.ToListAsync();
        
        // Assert - Only active entities should be returned
        Assert.Single(activeOnly);
        Assert.Equal("Active", activeOnly[0].Name);
        Assert.False(activeOnly[0].IsDeleted);
    }

    [Fact]
    public async Task QueryFilter_CanIncludeDeletedEntitiesWithIgnoreQueryFilters()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var activeEntity = new TestEntity { Id = 1, Name = "Active", IsDeleted = false };
        var deletedEntity = new TestEntity { Id = 2, Name = "Deleted", IsDeleted = true, DeletedAt = DateTime.UtcNow };

        context.TestEntities.AddRange(activeEntity, deletedEntity);
        await context.SaveChangesAsync();

        // Act - Query with IgnoreQueryFilters to get all entities including deleted
        var allEntitiesIncludingDeleted = await context.TestEntities
            .IgnoreQueryFilters()
            .ToListAsync();

        // Assert - Should return both active and deleted entities
        Assert.Equal(2, allEntitiesIncludingDeleted.Count);
        Assert.Contains(allEntitiesIncludingDeleted, e => e.Name == "Active" && !e.IsDeleted);
        Assert.Contains(allEntitiesIncludingDeleted, e => e.Name == "Deleted" && e.IsDeleted);
    }

    [Fact]
    public async Task QueryFilter_DoesNotAffectNonSoftDeletableEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var regularEntity = new RegularEntity { Id = 1, Name = "Regular" };
        context.RegularEntities.Add(regularEntity);
        await context.SaveChangesAsync();

        // Act - Query regular entities
        var allRegular = await context.RegularEntities.ToListAsync();

        // Assert - Regular entities should not have query filters applied
        Assert.Single(allRegular);
        Assert.Equal("Regular", allRegular[0].Name);
    }
}

