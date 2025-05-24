using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace DevHabit.Api.Middlewares;

public sealed partial class ValidationExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        ProblemDetailsContext context = new()
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new()
            {
                Detail = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
            },
        };

        if (validationException.Errors.Any())
        {
            Dictionary<string, string[]> errors = validationException.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => ToCamelCase(g.Key),
                    g => g.Select(x => x.ErrorMessage).ToArray());

            context.ProblemDetails.Extensions.Add("errors", errors);
        }
        else
        {
            context.ProblemDetails.Detail = validationException.Message;
        }

        return await problemDetailsService.TryWriteAsync(context);
    }

    /// <summary>
    /// Converts a string in PascalCase format to camelCase,
    /// keeping the nested property structure (separated by dot)
    /// </summary>
    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        string result = char.ToLowerInvariant(value[0]) + value[1..];

        return PropertyDotSeparatorRegex()
            .Replace(result, m => "." + char.ToLowerInvariant(m.Groups[1].Value[0]));
    }

    [GeneratedRegex(@"\.(\w)", RegexOptions.None, 20, "en-US")]
    private static partial Regex PropertyDotSeparatorRegex();
}
