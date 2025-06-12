using DevHabit.API.Entities;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;

namespace DevHabit.API.Services.Sorting;

public static class QueryableExtensions
{
    public static IQueryable<T> QueryHasValue<T>(
    this IQueryable<T> source,
    bool condition,
    Expression<Func<T, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sort,
        SortMapping[] mappings, 
        string? defaultOrderBy = "Id")
    {
        if (string.IsNullOrWhiteSpace(sort))
        { 
            return query.OrderBy(defaultOrderBy!);
        }

        string[] sortFields = sort.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        var orderByParts = new List<string>();
        foreach (string field in sortFields)
        {
           (string sortField, bool isDescending) = ParsedField(field);

            SortMapping mapping = mappings.First(m =>
                m.SortField.Equals(sortField, StringComparison.OrdinalIgnoreCase));

            string direction = (isDescending, mapping.Reverse) switch
            {
                (false, false) => "ASC",
                (false, true) => "DESC",
                (true, false) => "DESC",
                (true, true) => "ASC",
            };

            orderByParts.Add($"{mapping.PropertyName} {direction}");
        }

        string orderBy = string.Join(",", orderByParts);

        return query.OrderBy(orderBy);
    }

    private static (string SortField, bool isDescending) ParsedField(string field)
    {
        string[] parts = field.Split(' ');
        string sortField = parts[0];
        bool isDescending = parts.Length > 1 &&
                            parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortField, isDescending);
    }
}
