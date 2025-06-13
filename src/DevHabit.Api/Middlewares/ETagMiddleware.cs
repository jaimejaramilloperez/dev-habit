using System.Buffers;
using System.Security.Cryptography;
using DevHabit.Api.Services;
using Microsoft.IO;

namespace DevHabit.Api.Middlewares;

public static partial class MiddlewareExtensions
{
    public static IApplicationBuilder UseEtagCaching(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ETagMiddleware>();
    }
}

public sealed class ETagMiddleware(RequestDelegate next)
{
    private static readonly string[] ConcurrencyCheckMethods =
    [
        HttpMethods.Patch,
        HttpMethods.Put,
    ];

    public async Task InvokeAsync(
        HttpContext context,
        RecyclableMemoryStreamManager streamManager)
    {
        if (CanSkipETag(context))
        {
            await next(context);
            return;
        }

        string resourcePath = context.Request.Path.Value ?? "/";
        Uri resourceUri = new(resourcePath, UriKind.Relative);

        string? clientETag = context.Request.Headers.IfNoneMatch.FirstOrDefault()?.Trim('"');
        string? ifMatch = context.Request.Headers.IfMatch.FirstOrDefault()?.Trim('"');

        if (ConcurrencyCheckMethods.Contains(context.Request.Method) && !string.IsNullOrWhiteSpace(ifMatch))
        {
            string currentETag = InMemoryETagStore.GetETag(resourceUri);

            if (!string.IsNullOrWhiteSpace(currentETag) && ifMatch != currentETag)
            {
                context.Response.StatusCode = StatusCodes.Status412PreconditionFailed;
                context.Response.ContentLength = 0;
                return;
            }
        }

        Stream originalStream = context.Response.Body;
        using RecyclableMemoryStream memoryStream = streamManager.GetStream();

        try
        {
            context.Response.Body = memoryStream;

            await next(context);

            if (IsETaggableResponse(context))
            {
                memoryStream.Position = 0;

                ReadOnlySequence<byte> responseBody = memoryStream.GetReadOnlySequence();
                string etag = GenerateEtag(responseBody);

                InMemoryETagStore.SetETag(resourceUri, etag);
                context.Response.Headers.ETag = $"\"{etag}\"";

                if (context.Request.Method == HttpMethods.Get && clientETag is not null && clientETag == etag)
                {
                    context.Response.StatusCode = StatusCodes.Status304NotModified;
                    context.Response.Body = originalStream;
                    context.Response.ContentLength = 0;
                    return;
                }
            }

            context.Response.Body = originalStream;
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalStream);
        }
        catch
        {
            context.Response.Body = originalStream;
            throw;
        }
    }

    private static bool CanSkipETag(HttpContext context)
    {
        return context.Request.Method == HttpMethods.Post ||
            context.Request.Method == HttpMethods.Delete;
    }

    private static bool IsETaggableResponse(HttpContext context)
    {
        return context.Response.StatusCode == StatusCodes.Status200OK &&
            (context.Response.Headers.ContentType.FirstOrDefault()?.Contains("json", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string GenerateEtag(ReadOnlySequence<byte> content)
    {
        using var sha256Hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        foreach (var memory in content)
        {
            sha256Hasher.AppendData(memory.Span);
        }

        byte[] hash = sha256Hasher.GetHashAndReset();
        return Convert.ToHexString(hash);
    }
}
