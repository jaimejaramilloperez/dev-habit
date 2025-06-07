using DevHabit.Api;
using DevHabit.Api.Configurations;
using DevHabit.Api.Database;
using DevHabit.Api.Extensions;
using DevHabit.Api.Middlewares;
using HealthChecks.UI.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiServices();
builder.AddErrorHandling();
builder.AddDatabase();
builder.AddObservability();
builder.AddApplicationServices();
builder.AddAuthenticationServices();
builder.AddBackgroundJobs();
builder.AddCorsPolicy();
builder.AddRateLimiting();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.ApplyMigrationsAsync<ApplicationDbContext>();
    await app.ApplyMigrationsAsync<ApplicationIdentityDbContext>();
    await app.SeedInitialDataAsync();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseCors(CorsOptions.PolicyName);

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.UseEtagCaching();

app.MapHealthChecks("health", new()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapControllers();

app.Run();
