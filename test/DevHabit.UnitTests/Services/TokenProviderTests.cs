using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Configurations;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DevHabit.UnitTests.Services;

public sealed class TokenProviderTests
{
    private readonly TokenProvider _sut;
    private readonly JwtAuthOptions _jwtAuthOptions;

    public TokenProviderTests()
    {
        IOptions<JwtAuthOptions> options = Options.Create(new JwtAuthOptions()
        {
            Key = "your-secret-key-here-that-should-also-be-fairly-long",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationInMinutes = 30,
            RefreshTokenExpirationInDays = 7,
        });

        _jwtAuthOptions = options.Value;
        _sut = new(options);
    }

    [Fact]
    public void Create_ShouldReturnAccessTokens()
    {
        // Arrange
        TokenRequestDto dto = new()
        {
            UserId = User.CreateNewId(),
            Email = "test@example.com",
            Roles = [Roles.Member],
        };

        // Act
        AccessTokensDto accessTokensDto = _sut.Create(dto);

        // Assert
        Assert.NotNull(accessTokensDto.AccessToken);
        Assert.NotNull(accessTokensDto.RefreshToken);
    }

    [Fact]
    public void Create_ShouldGenerateValidAccessToken()
    {
        // Arrange
        TokenRequestDto dto = new()
        {
            UserId = User.CreateNewId(),
            Email = "test@example.com",
            Roles = [Roles.Member],
        };

        // Act
        AccessTokensDto accessTokensDto = _sut.Create(dto);

        // Assert
        JwtSecurityTokenHandler handler = new()
        {
            MapInboundClaims = false,
        };

        TokenValidationParameters validationParameters = new()
        {
            ValidIssuer = _jwtAuthOptions.Issuer,
            ValidAudience = _jwtAuthOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtAuthOptions.Key)),
            ValidateIssuerSigningKey = true,
            NameClaimType = JwtRegisteredClaimNames.Email,
            RoleClaimType = JwtCustomClaimNames.Role,
        };

        ClaimsPrincipal claimsPrincipal = handler.ValidateToken(
            accessTokensDto.AccessToken,
            validationParameters,
            out SecurityToken validatedToken);

        Assert.NotNull(validatedToken);
        Assert.Equal(dto.UserId, claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub));
        Assert.Equal(dto.Email, claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Email));
        Assert.Contains(claimsPrincipal.FindAll(JwtCustomClaimNames.Role), claim => claim.Value == Roles.Member);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueRefreshTokens()
    {
        // Arrange
        TokenRequestDto dto = new()
        {
            UserId = User.CreateNewId(),
            Email = "test@example.com",
            Roles = [Roles.Member],
        };

        // Act
        AccessTokensDto accessTokensDto1 = _sut.Create(dto);
        AccessTokensDto accessTokensDto2 = _sut.Create(dto);

        // Assert
        Assert.NotEqual(accessTokensDto1.RefreshToken, accessTokensDto2.RefreshToken);
    }

    [Fact]
    public void Create_ShouldGenerateAccessTokenWithCorrectExpiration()
    {
        // Arrange
        TokenRequestDto dto = new()
        {
            UserId = User.CreateNewId(),
            Email = "test@example.com",
            Roles = [Roles.Member],
        };

        // Act
        AccessTokensDto accessTokensDto = _sut.Create(dto);

        // Assert
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken jwtSecurityToken = handler.ReadJwtToken(accessTokensDto.AccessToken);

        DateTime expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtAuthOptions.ExpirationInMinutes);
        DateTime actualExpiration = jwtSecurityToken.ValidTo;

        // Allow for a small time difference due to test execution
        Assert.True(Math.Abs((expectedExpiration - actualExpiration).TotalSeconds) < 3);
    }

    [Fact]
    public void Create_ShouldGenerateBase64RefreshToken()
    {
        // Arrange
        TokenRequestDto dto = new()
        {
            UserId = User.CreateNewId(),
            Email = "test@example.com",
            Roles = [Roles.Member],
        };

        // Act
        AccessTokensDto accessTokensDto = _sut.Create(dto);

        // Assert
        Assert.True(IsBase64String(accessTokensDto.RefreshToken));
    }

    private static bool IsBase64String(string base64)
    {
        Span<byte> buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64, buffer, out _);
    }
}
