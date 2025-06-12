using DevHabit.API.Entities;
using System.Linq.Expressions;

namespace DevHabit.API.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> QueryHasValue<T>(
    this IQueryable<T> source,
    bool condition,
    Expression<Func<T, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }
}
