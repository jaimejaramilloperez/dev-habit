using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Configurations;
using DevHabit.Api.Database;
using DevHabit.Api.Extensions;
using DevHabit.Api.Jobs.EntryImport;
using DevHabit.Api.Jobs.GitHub;
using DevHabit.Api.Middlewares;
using DevHabit.Api.Services;
using DevHabit.Api.Services.GitHub;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IO;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Quartz;
using Refit;

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

        builder.Services.AddResponseCaching();

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

        // builder.Services.AddTransient<DelayHandler>();

        builder.Services.AddScoped<GitHubService>();

        builder.Services.AddScoped<RefitGitHubService>();

        builder.Services.AddScoped<GitHubAccessTokenService>();

        builder.Services.AddHttpClient()
            .ConfigureHttpClientDefaults(options => options.AddStandardResilienceHandler());

        builder.Services.AddHttpClient("github", client =>
        {
            client.BaseAddress = new(builder.Configuration.GetValue<string>("GitHub:BaseUrl")!);
            client.DefaultRequestHeaders.Accept.Add(new("application/vnd.github+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new("DevHabit", "1.0"));
        });

        builder.Services.AddRefitClient<IGitHubApi>(new()
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(new()
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            }),
        })
        .ConfigureHttpClient(client =>
        {
            client.BaseAddress = new(builder.Configuration.GetValue<string>("GitHub:BaseUrl")!);
        });
        // .AddHttpMessageHandler<DelayHandler>();
        // .InternalRemoveAllResilienceHandlers()
        // .AddResilienceHandler("custom", pipeline =>
        // {
        //     pipeline.AddTimeout(TimeSpan.FromSeconds(5));

        //     pipeline.AddRetry(new()
        //     {
        //         MaxRetryAttempts = 3,
        //         BackoffType = DelayBackoffType.Exponential,
        //         UseJitter = true,
        //         Delay = TimeSpan.FromMilliseconds(500),
        //     });

        //     pipeline.AddCircuitBreaker(new()
        //     {
        //         SamplingDuration = TimeSpan.FromSeconds(10),
        //         FailureRatio = 0.9,
        //         MinimumThroughput = 5,
        //         BreakDuration = TimeSpan.FromSeconds(5),
        //     });

        //     pipeline.AddTimeout(TimeSpan.FromSeconds(1));
        // });

        builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection("Encryption"));

        builder.Services.AddSingleton<EncryptionService>();

        builder.Services.AddSingleton<RecyclableMemoryStreamManager>();

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

    public static WebApplicationBuilder AddBackgroundJobs(this WebApplicationBuilder builder)
    {
        builder.Services.AddQuartz(configurator =>
        {
            // GitHub automation scheduler
            configurator.AddJob<GitHubAutomationSchedulerJob>(options => options.WithIdentity("github-automation-scheduler"));

            configurator.AddTrigger(options =>
            {
                options.ForJob("github-automation-scheduler")
                    .WithIdentity("github-automation-scheduler-trigger")
                    .WithSimpleSchedule(scheduleBuilder =>
                    {
                        GitHubAutomationOptions settings = builder.Configuration
                            .GetSection(GitHubAutomationOptions.SectionName)
                            .Get<GitHubAutomationOptions>()!;

                        scheduleBuilder.WithIntervalInMinutes(settings.ScanIntervalInMinutes)
                            .RepeatForever();
                    });
            });

            // Entry import clean up - runs daily at 3 AM UTC
            configurator.AddJob<CleanUpEntryImportJob>(options => options.WithIdentity("cleanup-entry-imports"));

            configurator.AddTrigger(options =>
            {
                options.ForJob("cleanup-entry-imports")
                    .WithIdentity("cleanup-entry-imports-trigger")
                    .WithCronSchedule("0 0 3 * * ?", x => x.InTimeZone(TimeZoneInfo.Utc));
            });
        });

        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return builder;
    }

    public static WebApplicationBuilder AddCorsPolicy(this WebApplicationBuilder builder)
    {
        CorsOptions corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()!;

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                policy.WithOrigins([.. corsOptions.AllowedOrigins])
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return builder;
    }

    public static WebApplicationBuilder AddRateLimiting(this WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = $"{retryAfter.TotalSeconds}";

                    ProblemDetailsFactory problemDetailsFactory = context.HttpContext.RequestServices
                        .GetRequiredService<ProblemDetailsFactory>();

                    Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(
                        httpContext: context.HttpContext,
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too Many Requests",
                        detail: $"Too Many Requests. Please try again after {retryAfter.TotalSeconds} seconds");

                    await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                }
            };

            options.AddPolicy("default", httpContext =>
            {
                string? identityId = httpContext.User.GetIdentityId();

                if (!string.IsNullOrWhiteSpace(identityId))
                {
                    return RateLimitPartition.GetTokenBucketLimiter(identityId, _ => new()
                    {
                        TokenLimit = 100,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 25,
                        QueueLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    });
                }

                return RateLimitPartition.GetFixedWindowLimiter("anonymous", _ => new()
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                });
            });
        });

        return builder;
    }
}

