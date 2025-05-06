using System.Linq.Dynamic.Core;
using DevHabit.Api.Dtos.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> OrderByQueryString<T>(
        this IQueryable<T> query,
        string? sort,
        IEnumerable<string> validFields,
        string? defaultOrderField = null)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return ApplyDefaultOrder(query, defaultOrderField);
        }

        string[] sortFields = sort
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        List<string> sortClauses = new(sortFields.Length);

        foreach (var field in sortFields)
        {
            string sortDirection = field[0] == '-' ? "desc" : "asc";
            string fieldName = sortDirection == "desc" ? field[1..] : field;

            if (validFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
            {
                sortClauses.Add($"{fieldName} {sortDirection}");
            }
        }

        string orderQuery = string.Join(',', sortClauses);

        return string.IsNullOrWhiteSpace(orderQuery)
            ? ApplyDefaultOrder(query, defaultOrderField)
            : query.OrderBy(orderQuery);
    }

    public static async Task<PaginationResult<T>> ToPaginationResult<T>(
        this IQueryable<T> query,
        int page,
        int pageSize)
    {
        long totalCount = await query.LongCountAsync();

        List<T> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new()
        {
            Data = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    private static IQueryable<T> ApplyDefaultOrder<T>(IQueryable<T> query, string? defaultOrderField)
    {
        return string.IsNullOrWhiteSpace(defaultOrderField)
            ? query
            : query.OrderBy(defaultOrderField.ToLowerInvariant());
    }
}
