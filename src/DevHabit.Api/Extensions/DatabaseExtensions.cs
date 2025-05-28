using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Extensions;

public static class DatabaseExtensions
{
    public static void ApplyMigrations<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        using IServiceScope scope = app.Services.CreateScope();
        using TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        try
        {
            dbContext.Database.Migrate();
            app.Logger.LogInformation("Database migrations for {DbContext} applied successfully", typeof(TDbContext).Name);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while applying database migrations for {DbContext}", typeof(TDbContext).Name);
            throw;
        }
    }

    public static async Task ApplyMigrationsAsync<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        using TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        try
        {
            await dbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Database migrations for {DbContext} applied successfully", typeof(TDbContext).Name);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while applying database migrations for {DbContext}", typeof(TDbContext).Name);
            throw;
        }
    }
}
