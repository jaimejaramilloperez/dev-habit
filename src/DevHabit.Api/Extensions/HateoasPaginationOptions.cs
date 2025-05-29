using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Extensions;

public sealed class HateoasPaginationOptions<T>
{
    public Func<T, ICollection<LinkDto>>? ItemLinksFactory { get; set; }
    public Func<ShapedPaginationResult<T>, ICollection<LinkDto>>? CollectionLinksFactory { get; set; }
    public string? AcceptHeader { get; set; }

    public void Deconstruct(
        out Func<T, ICollection<LinkDto>>? itemLinksFactory,
        out Func<ShapedPaginationResult<T>, ICollection<LinkDto>>? collectionLinksFactory,
        out string? acceptHeader)
    {
        itemLinksFactory = ItemLinksFactory;
        collectionLinksFactory = CollectionLinksFactory;
        acceptHeader = AcceptHeader;
    }
}
