using System.Diagnostics;
using DevHabit.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevHabit.Api.Common.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class RequireUserIdAttribute : ActionFilterAttribute
{
    private static readonly ActivitySource ActivitySource = new("DevHabit.Tracing");

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        using (var activity = ActivitySource.StartActivity("Check User"))
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
        }

        ILogger<RequireUserIdAttribute> logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireUserIdAttribute>>();
        using var scope = logger.BeginScope("{UserId}", context.HttpContext.Items[AuthConstants.UserId]);

        await next();
    }
}
