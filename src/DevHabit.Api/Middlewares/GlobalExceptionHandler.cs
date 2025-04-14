using Microsoft.AspNetCore.Diagnostics;

namespace DevHabit.Api.Middlewares;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return problemDetailsService.TryWriteAsync(new()
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new()
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request.",
            },
        });
    }
}
