using DevHabit.Api.Common.Auth;
using Microsoft.AspNetCore.Identity;
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

    public static async Task SeedInitialDataAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            if (!await roleManager.RoleExistsAsync(Roles.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            }

            if (!await roleManager.RoleExistsAsync(Roles.Member))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Member));
            }

            app.Logger.LogInformation("Roles created successfully");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while seeding initial data");
            throw;
        }
    }
}
