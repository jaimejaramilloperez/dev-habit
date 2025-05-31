using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DevHabit.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetIdentityId(this ClaimsPrincipal? claimsPrincipal)
    {
        string? identityId = claimsPrincipal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return identityId;
    }
}
