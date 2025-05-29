using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Services;

public sealed class LinkService(
    LinkGenerator linkGenerator,
    IHttpContextAccessor httpContextAccessor)
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

        return string.IsNullOrWhiteSpace(href)
            ? throw new InvalidOperationException("The provided endpoint name is invalid")
            : new(href, rel, method);
    }
}
