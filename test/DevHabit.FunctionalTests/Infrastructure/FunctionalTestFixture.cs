using System.Net.Http.Json;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DevHabit.FunctionalTests.Infrastructure;

public abstract class FunctionalTestFixture(DevHabitWebAppFactory appFactory)
    : IClassFixture<DevHabitWebAppFactory>
{
    private HttpClient? _authenticatedClient;

    public HttpClient CreateClient() => appFactory.CreateClient();

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = $"test-user@example.com",
        string password = "StrongPass12345!",
        string name = "test-user",
        bool forceNewClient = false)
    {
        if (_authenticatedClient is not null && !forceNewClient)
        {
            return _authenticatedClient;
        }

        HttpClient client = CreateClient();

        bool userExists = false;

        await using (AsyncServiceScope serviceScope = appFactory.Services.CreateAsyncScope())
        {
            using ApplicationDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            userExists = await dbContext.Users.AnyAsync(x => x.Email == email);
        }

        if (!userExists)
        {
            HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
                Routes.AuthRoutes.Register,
                new RegisterUserDto()
                {
                    Name = name,
                    Email = email,
                    Password = password,
                    ConfirmationPassword = password,
                });

            registerResponse.EnsureSuccessStatusCode();
        }

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Login, new LoginUserDto()
        {
            Email = email,
            Password = password,
        });

        loginResponse.EnsureSuccessStatusCode();

        AccessTokensDto? accessTokens = await loginResponse.Content.ReadFromJsonAsync<AccessTokensDto>();

        if (accessTokens?.AccessToken is null)
        {
            throw new InvalidOperationException("Failed to get authentication token");
        }

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessTokens.AccessToken);

        _authenticatedClient = client;

        return client;
    }

    public async Task CleanUpDatabaseAsync()
    {
        await using AsyncServiceScope serviceScope = appFactory.Services.CreateAsyncScope();
        IConfiguration configuration = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();

        string? connectionString = configuration.GetConnectionString("Database");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string not found in configuration");
        }

        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();

        await using NpgsqlCommand command = new("""
            DO $$
            BEGIN
                TRUNCATE TABLE dev_habit.entries CASCADE;
                TRUNCATE TABLE dev_habit.entry_import_jobs CASCADE;
                TRUNCATE TABLE dev_habit.tags CASCADE;
                TRUNCATE TABLE dev_habit.habits CASCADE;
                TRUNCATE TABLE dev_habit.users CASCADE;

                TRUNCATE TABLE identity.asp_net_users CASCADE;
                TRUNCATE TABLE identity.refresh_tokens CASCADE;
            END $$;
        """, connection);

        await command.ExecuteNonQueryAsync();
    }
}
