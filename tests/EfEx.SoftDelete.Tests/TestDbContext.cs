using Microsoft.EntityFrameworkCore;
using EfEx.SoftDelete.Extensions;

namespace EfEx.SoftDelete.Tests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities { get; set; } = null!;
    public DbSet<RegularEntity> RegularEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Enable soft delete query filter for testing
        modelBuilder.EnableSoftDelete();
    }
}

