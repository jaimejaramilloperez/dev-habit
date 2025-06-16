using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace DevHabit.Api.Configurations.Swagger;

public sealed class ConfigureSwaggerUIOptions(IApiVersionDescriptionProvider apiVersionDescriptionProvider)
    : IConfigureOptions<SwaggerUIOptions>
{
    public void Configure(SwaggerUIOptions options)
    {
        IEnumerable<string> descriptions = apiVersionDescriptionProvider.ApiVersionDescriptions
            .OrderBy(x => x.ApiVersion)
            .Select(x => x.GroupName);

        foreach (string groupName in descriptions)
        {
            options.SwaggerEndpoint($"/swagger/{groupName}/swagger.json", groupName);
        }

        options.DocumentTitle = "DevHabit Api - Swagger Docs";

        options.DisplayRequestDuration();
    }
}
