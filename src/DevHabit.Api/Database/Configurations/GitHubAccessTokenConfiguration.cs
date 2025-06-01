using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Api.Database.Configurations;

public sealed class GitHubAccessTokenConfiguration : IEntityTypeConfiguration<GitHubAccessToken>
{
    public void Configure(EntityTypeBuilder<GitHubAccessToken> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(500);

        builder.Property(x => x.UserId).HasMaxLength(500);

        builder.Property(x => x.Token).HasMaxLength(1000);

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<GitHubAccessToken>(x => x.UserId);
    }
}
