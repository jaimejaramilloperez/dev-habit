using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace DevHabit.Api.Configurations.Scalar;

public sealed class ConfigureScalarOptions(IApiVersionDescriptionProvider apiVersionDescriptionProvider)
    : IConfigureOptions<ScalarOptions>
{
    public void Configure(ScalarOptions options)
    {
        IEnumerable<string> descriptions = apiVersionDescriptionProvider.ApiVersionDescriptions
            .OrderBy(x => x.ApiVersion)
            .Select(x => x.GroupName);

        options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
        options.AddDocuments(descriptions);

        options.WithTitle("DevHabit Api - Scalar Docs");
    }
}
