using System.Security.Cryptography;
using DevHabit.Api;
using DevHabit.IntegrationTests.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace DevHabit.IntegrationTests.Infrastructure;

public sealed class DevHabitWebAppFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17.5-alpine3.21")
        .WithDatabase("devhabit")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly GitHubApiServer _gitHubApiServer = new();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        _gitHubApiServer.Start();
        _gitHubApiServer.SetUpValidUser();
    }

    public new async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
        await _postgreSqlContainer.DisposeAsync();

        _gitHubApiServer.Stop();
        _gitHubApiServer.Dispose();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Database", _postgreSqlContainer.GetConnectionString());
        builder.UseSetting("Encryption:Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        builder.UseSetting("GitHub:BaseUrl", _gitHubApiServer.Url);
    }
}
