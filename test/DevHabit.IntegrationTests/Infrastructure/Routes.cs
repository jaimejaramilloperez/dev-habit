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

    public static class EntryImportJobRoutes
    {
        public const string GetAll = $"{Base}/entries/imports";
        public const string Get = $"{Base}/entries/imports";
        public const string Create = $"{Base}/entries/imports";
    }

    public static class GitHubRoutes
    {
        public const string GetProfile = $"{Base}/github/profile";
        public const string GetEvents = $"{Base}/github/events";
        public const string StorePersonalAccessToken = $"{Base}/github/personal-access-token";
        public const string RevokePersonalAccessToken = $"{Base}/github/personal-access-token";
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

    public static class TagRoutes
    {
        public const string GetAll = $"{Base}/tags";
        public const string Get = $"{Base}/tags";
        public const string Create = $"{Base}/tags";
        public const string Update = $"{Base}/tags";
        public const string Delete = $"{Base}/tags";
    }

    public static class UserRoutes
    {
        public const string CurrentUser = $"{Base}/users/me";
        public const string UpdateProfile = $"{Base}/users/me/profile";
    }
}
