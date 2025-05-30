using System.Dynamic;
using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Extensions;

public static class HateoasExtensions
{
    public static async Task<ShapedResult?> WithHateoasAsync(
        this Task<ShapedResult?> resultTask,
        ICollection<LinkDto> links,
        string? acceptHeader)
    {
        ShapedResult? result = await resultTask;

        if (result is not null && HateoasHelpers.ShouldIncludeHateoas(acceptHeader))
        {
            result.Item.TryAdd(HateoasPropertyNames.Links, links);
        }

        return result;
    }

    public static async Task<ShapedPaginationResult<T>> WithHateoasAsync<T>(
        this Task<ShapedPaginationResult<T>> resultTask,
        HateoasPaginationOptions<T> options)
    {
        var (itemLinksFactory, collectionLinksFactory, acceptHeader) = options;

        ShapedPaginationResult<T> result = await resultTask;

        if (!HateoasHelpers.ShouldIncludeHateoas(acceptHeader))
        {
            return result;
        }

        List<LinkDto> links = [.. result.Links];

        if (collectionLinksFactory is not null)
        {
            links.AddRange(collectionLinksFactory(result));
        }

        List<ExpandoObject> data = [.. result.Data];

        if (itemLinksFactory is not null)
        {
            data = result.Data.Zip(result.OriginalData, (shapedItem, originalItem) =>
            {
                IDictionary<string, object?> newExpando = new ExpandoObject();

                foreach (var kvp in shapedItem)
                {
                    newExpando[kvp.Key] = kvp.Value;
                }

                newExpando.TryAdd(HateoasPropertyNames.Links, itemLinksFactory(originalItem));

                return (ExpandoObject)newExpando;
            }).ToList();
        }

        return new()
        {
            Data = data,
            OriginalData = result.OriginalData,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            Links = links,
        };
    }
}
