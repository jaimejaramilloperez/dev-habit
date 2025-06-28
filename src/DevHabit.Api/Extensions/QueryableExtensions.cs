using System.Diagnostics;
using System.Linq.Dynamic.Core;
using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Common.Pagination;
using DevHabit.Api.Common.Sorting;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Extensions;

public static class QueryableExtensions
{
    private static readonly ActivitySource ActivitySource = new("DevHabit.Tracing");

    public static IQueryable<T> SortByQueryString<T>(
        this IQueryable<T> query,
        string? sortExpression,
        ICollection<SortMapping> mappings,
        string? defaultOrderField = null)
    {
        if (string.IsNullOrWhiteSpace(sortExpression))
        {
            return ApplyDefaultOrder(query, defaultOrderField);
        }

        string[] sortFields = sortExpression
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToArray();

        if (!SortMapping.AreAllSortFieldsValid(mappings, sortFields))
        {
            throw new ValidationException([new("sort", $"Sort value '{sortExpression}' is not valid")]);
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

    public static async Task<PaginationResult<T>> ToPaginationResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        long totalCount = await query.LongCountAsync(cancellationToken);

        List<T> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new()
        {
            Data = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public static async Task<CollectionResult<T>> ToCursorPaginationResultAsync<T>(
        this IQueryable<T> query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        List<T> items = await query
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        return new()
        {
            Data = items,
        };
    }

    public static async Task<ShapedResult<T>?> ToShapedFirstOrDefaultAsync<T>(
        this IQueryable<T> query,
        string? fields,
        CancellationToken cancellationToken = default)
    {
        T? item = await query.FirstOrDefaultAsync(cancellationToken);

        return item is null
            ? null
            : new()
            {
                Item = DataShaper.ShapeData(item, fields),
                OriginalItem = item,
            };
    }

    public static async Task<ShapedPaginationResult<T>> ToShapedPaginationResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        string? fields,
        CancellationToken cancellationToken = default)
    {
        using (var activity = ActivitySource.StartActivity($"Get Pagination.Result.{typeof(T).Name}"))
        {
            activity?.SetTag("pagination.page", page);

            long totalCount = await query.LongCountAsync(cancellationToken);

            List<T> items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new()
            {
                Data = DataShaper.ShapeCollectionData(items, fields),
                OriginalData = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }
    }

    private static IQueryable<T> ApplyDefaultOrder<T>(IQueryable<T> query, string? defaultOrderField)
    {
        return string.IsNullOrWhiteSpace(defaultOrderField)
            ? query
            : query.OrderBy(defaultOrderField.ToLowerInvariant());
    }
}
