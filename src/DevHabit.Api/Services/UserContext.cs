using DevHabit.Api.Database;
using DevHabit.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DevHabit.Api.Services;

public class UserContext(
    ApplicationDbContext appDbContext,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache memoryCache)
{
    private const string CacheKeyPrefix = "users:id:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        string? identityId = httpContextAccessor.HttpContext?.User.GetIdentityId();

        if (string.IsNullOrWhiteSpace(identityId))
        {
            return null;
        }

        string cacheKey = $"{CacheKeyPrefix}{identityId}";

        string? userId = await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetSlidingExpiration(CacheDuration);

            string? userId = await appDbContext.Users
                .Where(x => x.IdentityId == identityId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return userId;
        });

        return userId;
    }
}
