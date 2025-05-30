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
        IValidator<RegisterUserDto> validator)
    {
        await validator.ValidateAndThrowAsync(registerUserDto);

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

        using IDbContextTransaction transaction = await _identityDbContext.Database.BeginTransactionAsync();

        _appDbContext.Database.SetDbConnection(_identityDbContext.Database.GetDbConnection());
        await _appDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        IdentityUser identityUser = new()
        {
            UserName = registerUserDto.Name,
            Email = registerUserDto.Email,
        };

        IdentityResult identityResult = await _userManager.CreateAsync(identityUser, registerUserDto.Password);

        if (!identityResult.Succeeded)
        {
            Dictionary<string, object?> extensions = new()
            {
                { "errors", identityResult.Errors.ToDictionary(x => x.Code, x => x.Description) }
            };

            return Problem(
                detail: "Unable to register the user, please try again",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions);
        }

        User user = registerUserDto.ToEntity();
        user.IdentityId = identityUser.Id;

        _appDbContext.Users.Add(user);

        await _appDbContext.SaveChangesAsync();

        TokenRequestDto tokenRequest = new(identityUser.Id, identityUser.Email);
        AccessTokensDto accessTokens = _tokenProvider.Create(tokenRequest);

        RefreshToken refreshToken = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessTokens.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationInDays),
        };

        _identityDbContext.RefreshTokens.Add(refreshToken);

        await _identityDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(accessTokens);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserDto loginUserDto)
    {
        IdentityUser? identityUser = await _userManager.FindByEmailAsync(loginUserDto.Email);

        if (identityUser is null || !await _userManager.CheckPasswordAsync(identityUser, loginUserDto.Password))
        {
            return Unauthorized();
        }

        TokenRequestDto tokenRequest = new(identityUser.Id, identityUser.Email!);
        AccessTokensDto accessTokens = _tokenProvider.Create(tokenRequest);

        RefreshToken refreshToken = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessTokens.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationInDays),
        };

        _identityDbContext.RefreshTokens.Add(refreshToken);

        await _identityDbContext.SaveChangesAsync();

        return Ok(accessTokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenDto refreshTokenDto)
    {
        RefreshToken? refreshToken = await _identityDbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshTokenDto.RefreshToken);

        if (refreshToken is null || refreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        TokenRequestDto tokenRequest = new(refreshToken.User.Id, refreshToken.User.Email!);
        AccessTokensDto accessTokens = _tokenProvider.Create(tokenRequest);

        refreshToken.Token = accessTokens.RefreshToken;
        refreshToken.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtAuthOptions.RefreshTokenExpirationInDays);

        await _identityDbContext.SaveChangesAsync();

        return Ok(accessTokens);
    }
}
