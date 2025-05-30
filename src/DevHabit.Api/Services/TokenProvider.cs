using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DevHabit.Api.Configurations;
using DevHabit.Api.Dtos.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DevHabit.Api.Services;

public sealed class TokenProvider(IOptions<JwtAuthOptions> options)
{
    private readonly JwtAuthOptions _jwtAuthOptions = options.Value;

    public AccessTokensDto Create(TokenRequestDto tokenRequest)
    {
        return new(GenerateToken(tokenRequest), GenerateRefreshToken());
    }

    private string GenerateToken(TokenRequestDto tokenRequest)
    {
        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(_jwtAuthOptions.Key));
        SigningCredentials signingCredentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, tokenRequest.UserId),
            new(JwtRegisteredClaimNames.Email, tokenRequest.Email),
        ];

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Issuer = _jwtAuthOptions.Issuer,
            Audience = _jwtAuthOptions.Audience,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = signingCredentials,
            Expires = DateTime.UtcNow.AddMinutes(_jwtAuthOptions.ExpirationInMinutes),
        };

        JsonWebTokenHandler handler = new();
        string accessToken = handler.CreateToken(tokenDescriptor);

        return accessToken;
    }

    private static string GenerateRefreshToken()
    {
        byte[] guidBytes = Encoding.UTF8.GetBytes(Guid.CreateVersion7().ToString());
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String([.. guidBytes, .. randomBytes]);
    }
}
