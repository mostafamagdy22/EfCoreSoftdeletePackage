using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using EfEx.SoftDelete.Models;

namespace EfEx.SoftDelete.Interceptors;

public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        ConvertHardDeleteToSoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ConvertHardDeleteToSoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ConvertHardDeleteToSoftDelete(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletetable)
            .ToList();

        foreach (var entry in entries)
        {
            entry.State = EntityState.Modified;
            if (entry.Entity is ISoftDeletetable softDeletable)
            {
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = DateTime.UtcNow;
            }
        }
    }
}