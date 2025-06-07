using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace DevHabit.Api.Common.Idempotency;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentRequestAttribute : Attribute, IAsyncActionFilter
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(60);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        bool hasHeader = context.HttpContext.Request.Headers.TryGetValue(
            IdempotencyKeyHeader,
            out StringValues idempotencyKeyValue);

        bool isValidGuid = Guid.TryParse(idempotencyKeyValue, out Guid idempotencyKey);

        if (!hasHeader || !isValidGuid)
        {
            ProblemDetailsFactory problemDetailsFactory = context.HttpContext.RequestServices
                .GetRequiredService<ProblemDetailsFactory>();

            ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(
                httpContext: context.HttpContext,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: $"Invalid or missing {IdempotencyKeyHeader} header");

            context.Result = new BadRequestObjectResult(problemDetails);

            return;
        }

        IMemoryCache cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
        string cacheKey = $"idempotence:{idempotencyKey}";

        IdempotencyCacheEntry? cachedEntry = cache.Get<IdempotencyCacheEntry?>(cacheKey);

        if (cachedEntry is not null)
        {
            context.HttpContext.Response.StatusCode = cachedEntry.StatusCode;
            context.HttpContext.Response.Headers.Location = cachedEntry.LocationHeader;
            context.Result = cachedEntry.Result;
            return;
        }

        ActionExecutedContext executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult)
        {
            IdempotencyCacheEntry entry = new()
            {
                StatusCode = objectResult.StatusCode!.Value,
                LocationHeader = context.HttpContext.Response.Headers.Location.ToString(),
                Result = objectResult,
            };

            cache.Set(cacheKey, entry, DefaultCacheDuration);
        }
    }
}
