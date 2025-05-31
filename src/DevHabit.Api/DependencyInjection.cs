using System.Text;
using Asp.Versioning;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Configurations;
using DevHabit.Api.Database;
using DevHabit.Api.Middlewares;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DevHabit.Api;

internal static class DependencyInjectionExtensions
{
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseKestrel(options => options.AddServerHeader = false);

        builder.Host.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        builder.Services.AddControllers(options =>
        {
            options.ReturnHttpNotAcceptable = true;
        })
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        })
        .AddXmlSerializerFormatters();

        builder.Services.Configure<MvcOptions>(options =>
        {
            NewtonsoftJsonOutputFormatter formatter = options.OutputFormatters
                .OfType<NewtonsoftJsonOutputFormatter>()
                .First();

            formatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.JsonV1);
            formatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.JsonV2);
            formatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.HateoasJson);
            formatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.HateoasJsonV1);
            formatter.SupportedMediaTypes.Add(CustomMediaTypesNames.Application.HateoasJsonV2);
        });

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1.0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new MediaTypeApiVersionReader(),
                new MediaTypeApiVersionReaderBuilder()
                    .Template("application/vnd.dev-habit.hateoas.v{version}+json")
                    .Build());
        })
        .AddMvc();

        builder.Services.AddOpenApi();

        return builder;
    }

    public static WebApplicationBuilder AddErrorHandling(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
        });

        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options => options
            .UseNpgsql(builder.Configuration.GetConnectionString("Database"), npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Application))
            .UseSnakeCaseNamingConvention());

        builder.Services.AddDbContext<ApplicationIdentityDbContext>(options => options
            .UseNpgsql(builder.Configuration.GetConnectionString("Database"), npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Identity))
            .UseSnakeCaseNamingConvention());

        return builder;
    }

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithTracing(tracing => tracing
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddNpgsql())
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation())
            .UseOtlpExporter();

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        });

        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

        return builder;
    }

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddValidatorsFromAssemblyContaining<IApiMarker>();

        builder.Services.AddMemoryCache();

        builder.Services.AddScoped<LinkService>();

        builder.Services.AddScoped<TokenProvider>();

        builder.Services.AddScoped<UserContext>();

        return builder;
    }

    public static WebApplicationBuilder AddAuthenticationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationIdentityDbContext>();

        builder.Services.Configure<JwtAuthOptions>(builder.Configuration.GetSection("Jwt"));

        JwtAuthOptions jwtAuthOptions = builder.Configuration.GetSection("Jwt").Get<JwtAuthOptions>()!;

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new()
            {
                ValidIssuer = jwtAuthOptions.Issuer,
                ValidAudience = jwtAuthOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAuthOptions.Key)),
                ValidateIssuerSigningKey = true,
                NameClaimType = JwtRegisteredClaimNames.Email,
                RoleClaimType = JwtCustomClaimNames.Role,
            };
        });

        builder.Services.AddAuthorization();

        return builder;
    }
}

