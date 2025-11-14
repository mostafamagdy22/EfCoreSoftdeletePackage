using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using EfEx.SoftDelete.Models;

namespace EfEx.SoftDelete.Extensions;

public static class SoftDeleteExtensions
{
    public static void EnableSoftDelete(this ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletetable).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType)
                       .HasQueryFilter(GetDeletedFilter(entityType.ClrType));
            }
        }
    }

    private static LambdaExpression GetDeletedFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");

        // e.IsDeleted
        var prop = Expression.Property(
            Expression.Convert(parameter, typeof(ISoftDeletetable)),
            nameof(ISoftDeletetable.IsDeleted)
        );

        // Compare against false
        var body = Expression.Equal(prop, Expression.Constant(false));

        return Expression.Lambda(body, parameter);
    }
}