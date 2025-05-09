using System.Linq.Dynamic.Core;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> SortByQueryString<T>(
        this IQueryable<T> query,
        string? sort,
        ICollection<SortMapping> mappings,
        string? defaultOrderField = null)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return ApplyDefaultOrder(query, defaultOrderField);
        }

        string[] sortFields = sort
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToArray();

        if (!SortMapping.AreAllSortFieldsValid(mappings, sortFields))
        {
            throw new ValidationException([new("sort", $"Sort value '{sort}' is not valid")]);
        }

        List<string> sortClauses = new(sortFields.Length);

        foreach (var sortField in sortFields)
        {
            Sort.Direction direction = sortField[0] == '-'
                ? Sort.Direction.Desc
                : Sort.Direction.Asc;

            string field = direction == Sort.Direction.Desc
                ? sortField[1..]
                : sortField;

            SortMapping mapping = mappings.First(x =>
                string.Equals(x.SortField, field, StringComparison.OrdinalIgnoreCase));

            string sortDirection = (direction, mapping.Reverse) switch
            {
                (Sort.Direction.Asc, false) => "ASC",
                (Sort.Direction.Desc, false) => "DESC",
                (Sort.Direction.Asc, true) => "DESC",
                (Sort.Direction.Desc, true) => "ASC",
                _ => "ASC",
            };

            sortClauses.Add($"{mapping.PropertyName} {sortDirection}");
        }

        string orderQuery = string.Join(',', sortClauses);

        return string.IsNullOrWhiteSpace(orderQuery)
            ? ApplyDefaultOrder(query, defaultOrderField)
            : query.OrderBy(orderQuery);
    }

    public static async Task<ShapedResult?> ToShapedFirstOrDefaultAsync<T>(
        this IQueryable<T> query,
        IDataShapingService dataShaping,
        string? fields)
    {
        if (!dataShaping.AreAllFieldsValid<T>(fields))
        {
            throw new ValidationException([new("fields", $"Fields value '{fields}' is not valid")]);
        }

        T? item = await query.FirstOrDefaultAsync();

        return item is null
            ? null
            : new(dataShaping.ShapeData(item, fields));
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

    public static async Task<ShapedPaginationResult> ToShapedPaginationAsync<T>(
        this IQueryable<T> query,
        ShapedPaginationResultOptions options)
    {
        var (page, pageSize, fields, dataShaping) = options;

        long totalCount = await query.LongCountAsync();

        List<T> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (!dataShaping.AreAllFieldsValid<T>(fields))
        {
            throw new ValidationException([new("fields", $"Fields value '{fields}' is not valid")]);
        }

        return new()
        {
            Data = dataShaping.ShapeCollectionData(items, fields!),
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
