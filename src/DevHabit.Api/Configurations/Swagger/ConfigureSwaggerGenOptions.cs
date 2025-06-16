using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DevHabit.Api.Configurations.Swagger;

public sealed class ConfigureSwaggerGenOptions(IApiVersionDescriptionProvider apiVersionDescriptionProvider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (ApiVersionDescription description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            OpenApiInfo openApiInfo = new()
            {
                Title = $"DevHabit.Api v{description.ApiVersion}",
                Version = description.ApiVersion.ToString(),
            };

            options.SwaggerDoc(description.GroupName, openApiInfo);
        }

        options.ResolveConflictingActions(descriptor => descriptor.First());

        string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        options.IncludeXmlComments(xmlPath);

        options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
        options.DescribeAllParametersInCamelCase();

        options.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Description = "Introduce a valid token",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT",
        });

        options.AddSecurityRequirement(new()
        {
            {
                new()
                {
                    Reference = new()
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme,
                    }
                },
                []
            }
        });
    }
}
