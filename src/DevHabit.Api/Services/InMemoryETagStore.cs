using System.Collections.Concurrent;

namespace DevHabit.Api.Services;

public static class InMemoryETagStore
{
    private static readonly ConcurrentDictionary<string, string> ETags = [];

    public static string GetETag(Uri resourceUri)
    {
        return ETags.GetOrAdd(resourceUri.ToString(), _ => string.Empty);
    }

    public static string GetETag(string resourceUri)
    {
        return ETags.GetOrAdd(resourceUri, _ => string.Empty);
    }

    public static void SetETag(Uri resourceUri, string etag)
    {
        ETags.AddOrUpdate(resourceUri.ToString(), etag, (_, _) => etag);
    }

    public static void SetETag(string resourceUri, string etag)
    {
        ETags.AddOrUpdate(resourceUri, etag, (_, _) => etag);
    }

    public static void RemoveETag(Uri resourceUri)
    {
        ETags.TryRemove(resourceUri.ToString(), out _);
    }

    public static void RemoveETag(string resourceUri)
    {
        ETags.TryRemove(resourceUri, out _);
    }
}
