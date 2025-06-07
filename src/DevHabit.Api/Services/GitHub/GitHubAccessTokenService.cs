using DevHabit.Api.Database;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Services.GitHub;

public sealed class GitHubAccessTokenService(
    ApplicationDbContext appDbContext,
    EncryptionService encryptionService)
{
    public async Task<string?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        GitHubAccessToken? gitHubAccessToken = await GetAccessTokenAsync(userId, cancellationToken);

        if (gitHubAccessToken?.Token is null)
        {
            return null;
        }

        string decryptedToken = encryptionService.Decrypt(gitHubAccessToken.Token);

        return decryptedToken;
    }

    public async Task UpsertAsync(
        string userId,
        StoreGithubAccessTokenDto accessTokenDto,
        CancellationToken cancellationToken = default)
    {
        GitHubAccessToken? existingAccessToken = await GetAccessTokenAsync(userId, cancellationToken);

        string encryptedToken = encryptionService.Encrypt(accessTokenDto.AccessToken);

        if (existingAccessToken is not null)
        {
            existingAccessToken.Token = encryptedToken;
            existingAccessToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays);
        }
        else
        {
            GitHubAccessToken newGitHubAccessToken = new()
            {
                Id = $"gh_{Guid.CreateVersion7()}",
                UserId = userId,
                Token = encryptedToken,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays),
            };

            appDbContext.GitHubAccessTokens.Add(newGitHubAccessToken);
        }

        await appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(string userId, CancellationToken cancellationToken = default)
    {
        GitHubAccessToken? gitHubAccessToken = await GetAccessTokenAsync(userId, cancellationToken);

        if (gitHubAccessToken is null)
        {
            return;
        }

        appDbContext.GitHubAccessTokens.Remove(gitHubAccessToken);

        await appDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<GitHubAccessToken?> GetAccessTokenAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await appDbContext.GitHubAccessTokens
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }
}
