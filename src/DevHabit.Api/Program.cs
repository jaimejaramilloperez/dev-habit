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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.ApplyMigrations<ApplicationDbContext>();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.MapHealthChecks("health", new()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapControllers();

app.Run();
