using EfEx.SoftDelete.Models;

namespace EfEx.SoftDelete.Tests;

public class TestEntity : ISoftDeletetable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

public class RegularEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

