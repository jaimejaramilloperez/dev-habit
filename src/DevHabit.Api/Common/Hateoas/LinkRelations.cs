namespace DevHabit.Api.Common.Hateoas;

internal static class LinkRelations
{
    public const string Self = "self";
    public const string Create = "create";
    public const string Edit = "edit";
    public const string Update = "update";
    public const string Patch = "partial-update";
    public const string Delete = "delete";
    public const string PreviousPage = "previous-page";
    public const string NextPage = "next-page";
    public const string UpsertTags = "upsert-tags";
    public const string StoreToken = "store-token";
    public const string RevokeToken = "revoke-token";
    public const string UpdateProfile = "update-profile";
}
