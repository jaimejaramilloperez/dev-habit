using DevHabit.Api.Dtos.Auth;

namespace DevHabit.IntegrationTests.TestData;

public static class AuthTestData
{
    private const string ValidTestEmail = "test@example.com";
    private const string ValidTestPassword = "StrongPass12345!";

    public static RegisterUserDto ValidRegisterUserDto => new()
    {
        Name = ValidTestEmail,
        Email = ValidTestEmail,
        Password = ValidTestPassword,
        ConfirmationPassword = ValidTestPassword,
    };

    public static LoginUserDto ValidLoginUserDto => new()
    {
        Email = ValidTestEmail,
        Password = ValidTestPassword,
    };
}
