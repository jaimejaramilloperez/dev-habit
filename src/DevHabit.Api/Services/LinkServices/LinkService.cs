using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Services.LinkServices;

public sealed class LinkService(
    LinkGenerator linkGenerator,
    IHttpContextAccessor httpContextAccessor) : ILinkService
{
    public LinkDto Create(
        string endpointName,
        string rel,
        string method,
        object? values = null,
        string? controllerName = null)
    {
        string? href = linkGenerator.GetUriByAction(
            httpContextAccessor.HttpContext!,
            endpointName,
            controllerName,
            values);

        return new()
        {
            Href = href ?? throw new InvalidOperationException("The provided endpoint name is invalid"),
            Rel = rel,
            Method = method,
        };
    }
}
