using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Services.LinkServices;

public interface ILinkService
{
    public LinkDto Create(
        string endpointName,
        string rel,
        string method,
        object? values = null,
        string? controllerName = null);
}
