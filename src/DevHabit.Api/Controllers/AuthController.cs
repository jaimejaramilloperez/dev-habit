using DevHabit.Api.Common.Auth;
using DevHabit.Api.Configurations;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Dtos.Users;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(
    UserManager<IdentityUser> userManager,
    ApplicationDbContext appDbContext,
    ApplicationIdentityDbContext identityDbContext,
    TokenProvider tokenProvider,
    IOptions<JwtAuthOptions> options) : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly ApplicationDbContext _appDbContext = appDbContext;
    private readonly ApplicationIdentityDbContext _identityDbContext = identityDbContext;
    private readonly TokenProvider _tokenProvider = tokenProvider;
    private readonly JwtAuthOptions _jwtAuthOptions = options.Value;

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterUserDto registerUserDto,
        IValidator<RegisterUserDto> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(registerUserDto, cancellationToken);

        bool emailIsTaken = await _userManager.FindByEmailAsync(registerUserDto.Email) is not null;

        if (emailIsTaken)
        {
            return Problem(
                detail: $"Email '{registerUserDto.Email}' is already taken",
                statusCode: StatusCodes.Status409Conflict);
        }

        bool usernameIsTaken = await _userManager.FindByNameAsync(registerUserDto.Name) is not null;

        if (usernameIsTaken)
        {
            return Problem(
                detail: $"Username '{registerUserDto.Name}' is already taken",
                statusCode: StatusCodes.Status409Conflict);
        }

        using IDbContextTransaction transaction = await _identityDbContext.Database.BeginTransactionAsync(cancellationToken);

        _appDbContext.Database.SetDbConnection(_identityDbContext.Database.GetDbConnection());
        await _appDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);

        IdentityUser identityUser = new()
        {
            UserName = registerUserDto.Name,
            Email = registerUserDto.Email,
        };

        IdentityResult createUserResult = await _userManager.CreateAsync(identityUser, registerUserDto.Password);

        if (!createUserResult.Succeeded)
        {
            Dictionary<string, object?> extensions = new()
            {
                { "errors", createUserResult.Errors.ToDictionary(x => x.Code, x => x.Description) }
            };

            return Problem(
                detail: "Unable to register the user, please try again",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions);
        }

        IdentityResult addToRoleResult = await _userManager.AddToRoleAsync(identityUser, Roles.Member);

        if (!addToRoleResult.Succeeded)
        {
            Dictionary<string, object?> extensions = new()
            {
                { "errors", addToRoleResult.Errors.ToDictionary(x => x.Code, x => x.Description) }
            };

            return Problem(
                detail: "Unable to register the user, please try again",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions);
        }

        User user = registerUserDto.ToEntity();
        user.IdentityId = identityUser.Id;

        _appDbContext.Users.Add(user);

        await _appDbContext.SaveChangesAsync(cancellationToken);

        TokenRequestDto tokenRequest = new(identityUser.Id, identityUser.Email, [Roles.Member]);
        AccessTokensDto accessTokens = _tokenProvider.Create(tokenRequest);

        RefreshToken refreshToken = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessTokens.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationInDays),
        };

        _identityDbContext.RefreshTokens.Add(refreshToken);

        await _identityDbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Ok(accessTokens);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginUserDto loginUserDto,
        CancellationToken cancellationToken)
    {
        IdentityUser? identityUser = await _userManager.FindByEmailAsync(loginUserDto.Email);

        if (identityUser is null || !await _userManager.CheckPasswordAsync(identityUser, loginUserDto.Password))
        {
            return Unauthorized();
        }

        IList<string> roles = await _userManager.GetRolesAsync(identityUser);

        TokenRequestDto tokenRequest = new(identityUser.Id, identityUser.Email!, roles);
        AccessTokensDto accessTokens = _tokenProvider.Create(tokenRequest);

        RefreshToken refreshToken = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessTokens.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationInDays),
        };

        _identityDbContext.RefreshTokens.Add(refreshToken);

        await _identityDbContext.SaveChangesAsync(cancellationToken);

        return Ok(accessTokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenDto refreshTokenDto,
        CancellationToken cancellationToken)
    {
        RefreshToken? refreshToken = await _identityDbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshTokenDto.RefreshToken, cancellationToken);

        if (refreshToken is null || refreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        IList<string> roles = await _userManager.GetRolesAsync(refreshToken.User);

        TokenRequestDto tokenRequest = new(refreshToken.User.Id, refreshToken.User.Email!, roles);
        AccessTokensDto accessTokens = _tokenProvider.Create(tokenRequest);

        refreshToken.Token = accessTokens.RefreshToken;
        refreshToken.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtAuthOptions.RefreshTokenExpirationInDays);

        await _identityDbContext.SaveChangesAsync(cancellationToken);

        return Ok(accessTokens);
    }
}
