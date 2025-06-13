namespace DevHabit.IntegrationTests.Infrastructure;

public static class Routes
{
    private const string Base = "api";

    public static class AuthRoutes
    {
        public const string Register = $"{Base}/auth/register";
        public const string Login = $"{Base}/auth/login";
        public const string Refresh = $"{Base}/auth/refresh";
    }

    public static class HabitRoutes
    {
        public const string GetAll = $"{Base}/habits";
        public const string Get = $"{Base}/habits";
        public const string Create = $"{Base}/habits";
        public const string Patch = $"{Base}/habits";
        public const string Update = $"{Base}/habits";
        public const string Delete = $"{Base}/habits";
    }

    public static class GitHubRoutes
    {
        public const string GetProfile = $"{Base}/github/profile";
        public const string GetEvents = $"{Base}/github/events";
        public const string StorePersonalAccessToken = $"{Base}/github/personal-access-token";
        public const string RevokePersonalAccessToken = $"{Base}/github/personal-access-token";
    }
}
