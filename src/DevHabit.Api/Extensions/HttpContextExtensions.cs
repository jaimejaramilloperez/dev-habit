using DevHabit.Api.Common.Auth;

namespace DevHabit.Api.Extensions;

public static class HttpContextExtensions
{
    public static string GetUserId(this HttpContext context)
    {
        string userId = context.Items[AuthConstants.UserId]?.ToString()
            ?? throw new InvalidOperationException("UserId not found in context. Make sure RequireUserId attribute is applied.");

        return userId;
    }
}
