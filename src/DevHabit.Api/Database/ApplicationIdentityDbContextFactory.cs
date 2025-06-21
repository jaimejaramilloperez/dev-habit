using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DevHabit.Api.Database;

public sealed class ApplicationIdentityDbContextFactory : IDesignTimeDbContextFactory<ApplicationIdentityDbContext>
{
    public ApplicationIdentityDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ApplicationIdentityDbContext> optionsBuilder = new();

        optionsBuilder
            .UseNpgsql("CONNECTION_STRING", npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Identity))
            .UseSnakeCaseNamingConvention();

        return new ApplicationIdentityDbContext(optionsBuilder.Options);
    }
}
