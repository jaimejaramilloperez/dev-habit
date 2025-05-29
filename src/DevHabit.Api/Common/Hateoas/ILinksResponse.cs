using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Common.Hateoas;

public interface ILinksResponse
{
    public IReadOnlyCollection<LinkDto> Links { get; init; }
}
