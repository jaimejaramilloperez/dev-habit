using Microsoft.AspNetCore.Identity;

namespace DevHabit.Api.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string Token { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public IdentityUser User { get; set; } = null!;
}
