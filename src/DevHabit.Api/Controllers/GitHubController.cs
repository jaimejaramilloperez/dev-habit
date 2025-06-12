using DevHabit.Api.Common.Auth;
using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Services;
using DevHabit.Api.Services.GitHub;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/github")]
[Authorize(Roles = Roles.Member)]
public sealed class GitHubController(
    RefitGitHubService gitHubService,
    GitHubAccessTokenService gitHubAccessTokenService,
    UserContext userContext,
    LinkService linkService) : ControllerBase
{
    private readonly RefitGitHubService _gitHubService = gitHubService;
    private readonly GitHubAccessTokenService _gitHubAccessTokenService = gitHubAccessTokenService;
    private readonly UserContext _userContext = userContext;
    private readonly LinkService _linkService = linkService;

    [HttpGet("profile")]
    [Produces(CustomMediaTypesNames.Application.HateoasJson)]
    public async Task<IActionResult> GetUserProfile(
        AcceptHeaderDto acceptHeaderDto,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        string? accessToken = await _gitHubAccessTokenService.GetAsync(userId, cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return NotFound();
        }

        GitHubUserProfileDto? userProfile = await _gitHubService.GetUserProfileAsync(accessToken, cancellationToken);

        if (userProfile is null)
        {
            return NotFound();
        }

        if (HateoasHelpers.ShouldIncludeHateoas(acceptHeaderDto.Accept))
        {
            var userProfileWithLinks = DataShaper.ShapeData(userProfile, [
                _linkService.Create(nameof(GetUserProfile), LinkRelations.Self, HttpMethods.Get),
                _linkService.Create(nameof(StoreAccessToken), LinkRelations.StoreToken, HttpMethods.Put),
                _linkService.Create(nameof(RevokeAccessToken), LinkRelations.RevokeToken, HttpMethods.Delete),
            ]);

            return Ok(userProfileWithLinks);
        }

        return Ok(userProfile);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetUserEvents(
        [FromQuery] int page,
        [FromQuery(Name = "per_page")] int perPage,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        string? accessToken = await _gitHubAccessTokenService.GetAsync(userId, cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Unauthorized();
        }

        GitHubUserProfileDto? profile = await _gitHubService.GetUserProfileAsync(accessToken, cancellationToken);

        if (profile is null)
        {
            return NotFound();
        }

        IReadOnlyList<GitHubEventDto> events = await _gitHubService.GetUserEventsAsync(
            profile.Login,
            accessToken,
            page,
            perPage,
            cancellationToken);

        return Ok(events);
    }

    [HttpPut("personal-access-token")]
    public async Task<IActionResult> StoreAccessToken(
        StoreGithubAccessTokenDto storeGithubAccessTokenDto,
        IValidator<StoreGithubAccessTokenDto> validator,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(storeGithubAccessTokenDto, cancellationToken);

        await _gitHubAccessTokenService.StoreAsync(userId, storeGithubAccessTokenDto, cancellationToken);

        return NoContent();
    }

    [HttpDelete("personal-access-token")]
    public async Task<IActionResult> RevokeAccessToken(CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await _gitHubAccessTokenService.RevokeAsync(userId, cancellationToken);

        return NoContent();
    }
}
