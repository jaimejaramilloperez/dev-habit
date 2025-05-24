using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Common;

public interface ILinksResponse
{
    public List<LinkDto> Links { get; init; }
}
