namespace EfEx.SoftDelete.Models
{
    public interface ISoftDeletetable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        string? DeletedBy { get; set; }
    }
}