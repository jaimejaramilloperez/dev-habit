using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Common.Idempotency;

internal sealed record IdempotencyCacheEntry
{
    public required int StatusCode { get; init; }
    public required string LocationHeader { get; init; }
    public required ObjectResult Result { get; init; }
}
