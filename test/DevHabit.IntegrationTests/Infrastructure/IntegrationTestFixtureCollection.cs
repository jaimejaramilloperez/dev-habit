namespace DevHabit.IntegrationTests.Infrastructure;

[Collection(nameof(IntegrationTestCollection))]
public abstract class IntegrationTestFixtureCollection(DevHabitWebAppFactory appFactory)
{
    public HttpClient CreateClient() => appFactory.CreateClient();
}
