using DevHabit.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevHabit.Api.Common.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class RequireUserIdAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        CancellationToken cancellationToken = context.HttpContext.RequestAborted;

        UserContext userContext = context.HttpContext.RequestServices.GetRequiredService<UserContext>();

        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items[AuthConstants.UserId] = userId;
        await next();
    }
}
