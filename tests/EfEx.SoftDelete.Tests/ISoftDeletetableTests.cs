using EfEx.SoftDelete.Models;
using Xunit;

namespace EfEx.SoftDelete.Tests;

public class ISoftDeletetableTests
{
    [Fact]
    public void ISoftDeletetable_HasRequiredProperties()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.NotNull(entity);
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }

    [Fact]
    public void ISoftDeletetable_CanSetProperties()
    {
        // Arrange
        var entity = new TestEntity();
        var deletedAt = DateTime.UtcNow;
        const string deletedBy = "TestUser";

        // Act
        entity.IsDeleted = true;
        entity.DeletedAt = deletedAt;
        entity.DeletedBy = deletedBy;

        // Assert
        Assert.True(entity.IsDeleted);
        Assert.Equal(deletedAt, entity.DeletedAt);
        Assert.Equal(deletedBy, entity.DeletedBy);
    }

    [Fact]
    public void ISoftDeletetable_PropertiesAreNullable()
    {
        // Arrange
        var entity = new TestEntity
        {
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = "User"
        };

        // Act
        entity.DeletedAt = null;
        entity.DeletedBy = null;

        // Assert
        Assert.True(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }
}

