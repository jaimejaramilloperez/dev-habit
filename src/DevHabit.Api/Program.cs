using DevHabit.Api;
using DevHabit.Api.Database;
using DevHabit.Api.Extensions;
using HealthChecks.UI.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiServices();
builder.AddErrorHandling();
builder.AddDatabase();
builder.AddObservability();
builder.AddApplicationServices();
builder.AddAuthenticationServices();

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

app.UseAuthentication();

app.UseAuthorization();

app.MapHealthChecks("health", new()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapControllers();

app.Run();
