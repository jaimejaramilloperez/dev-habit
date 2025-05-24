using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Services.DataShapingServices;

namespace DevHabit.Api.Extensions;

public sealed record ShapedFirstOrDefaultOptions
{
    public string? Fields { get; init; }
    public string? AcceptHeader { get; init; }
    public required List<LinkDto> Links { get; init; }
    public required IDataShapingService DataShapingService { get; init; }

    public void Deconstruct(
        out string? fields,
        out string? acceptHeader,
        out List<LinkDto> links,
        out IDataShapingService dataShapingService)
    {
        fields = Fields;
        acceptHeader = AcceptHeader;
        links = Links;
        dataShapingService = DataShapingService;
    }
}
