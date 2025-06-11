using System.Security.Cryptography;
using DevHabit.Api.Configurations;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevHabit.UnitTests.Services.GitHub;

public sealed class GitHubAccessTokenServiceTests : IDisposable
{
    private readonly GitHubAccessTokenService _sut;
    private readonly ApplicationDbContext _dbContext;
    private readonly EncryptionService _encryptionService;

    public GitHubAccessTokenServiceTests()
    {
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new(dbContextOptions);

        IOptions<EncryptionOptions> encryptionOptions = Options.Create(new EncryptionOptions()
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        });

        _encryptionService = new(encryptionOptions);

        _sut = new(_dbContext, _encryptionService);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task StoreAsync_ShouldCreateNewToken_WhenUserDoesNotHaveOne()
    {
        // Arrange
        string userId = User.CreateNewId();

        StoreGithubAccessTokenDto dto = new()
        {
            AccessToken = "github-token",
            ExpiresInDays = 30,
        };

        // Act
        await _sut.StoreAsync(userId, dto);

        // Assert
        GitHubAccessToken? token = await _dbContext.GitHubAccessTokens.FirstOrDefaultAsync(x => x.UserId == userId);

        Assert.NotNull(token);
        Assert.Equal(userId, token.UserId);
        Assert.NotEqual(dto.AccessToken, token.Token);
        Assert.True(token.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task StoreAsync_ShouldUpdateExistingToken_WhenUserHaveOne()
    {
        // Arrange
        string userId = User.CreateNewId();

        GitHubAccessToken existingToken = new()
        {
            Id = GitHubAccessToken.CreateNewId(),
            UserId = userId,
            Token = "github-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(29),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
        };

        _dbContext.GitHubAccessTokens.Add(existingToken);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        StoreGithubAccessTokenDto dto = new()
        {
            AccessToken = "new-github-token",
            ExpiresInDays = 30,
        };

        // Act
        await _sut.StoreAsync(userId, dto);

        // Assert
        GitHubAccessToken? token = await _dbContext.GitHubAccessTokens.FirstOrDefaultAsync(x => x.UserId == userId);

        Assert.NotNull(token);
        Assert.Equal(existingToken.Id, token.Id);
        Assert.Equal(existingToken.UserId, token.UserId);
        Assert.NotEqual(existingToken.Token, token.Token);
        Assert.True(token.ExpiresAtUtc > existingToken.ExpiresAtUtc);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDecryptedToken_WhenTokenExists()
    {
        // Arrange
        string userId = User.CreateNewId();
        string originalToken = "github-token";

        GitHubAccessToken existingToken = new()
        {
            Id = GitHubAccessToken.CreateNewId(),
            UserId = userId,
            Token = _encryptionService.Encrypt(originalToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(29),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
        };

        _dbContext.GitHubAccessTokens.Add(existingToken);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        string? token = await _sut.GetAsync(userId);

        // Assert
        Assert.NotNull(token);
        Assert.Equal(originalToken, token);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenTokenDoesNotExist()
    {
        // Arrange
        string userId = User.CreateNewId();

        // Act
        string? token = await _sut.GetAsync(userId);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public async Task RevokeAsync_ShouldRemoveToken_WhenTokenExists()
    {
        // Arrange
        string userId = User.CreateNewId();

        GitHubAccessToken existingToken = new()
        {
            Id = GitHubAccessToken.CreateNewId(),
            UserId = userId,
            Token = "github-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(29),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
        };

        _dbContext.GitHubAccessTokens.Add(existingToken);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        await _sut.RevokeAsync(userId);

        // Assert
        bool tokenExists = await _dbContext.GitHubAccessTokens.AnyAsync(x => x.UserId == userId);
        Assert.False(tokenExists);
    }

    [Fact]
    public async Task RevokeAsync_ShouldNotThrow_WhenTokenDoesNotExist()
    {
        // Arrange
        string userId = User.CreateNewId();

        // Act
        await _sut.RevokeAsync(userId);

        // Assert
        bool tokenExists = await _dbContext.GitHubAccessTokens.AnyAsync(x => x.UserId == userId);
        Assert.False(tokenExists);
    }
}
