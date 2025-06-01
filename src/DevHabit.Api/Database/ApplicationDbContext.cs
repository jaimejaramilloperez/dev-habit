using DevHabit.Api.Database.Configurations;
using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Database;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<HabitTag> HabitTags => Set<HabitTag>();
    public DbSet<User> Users => Set<User>();
    public DbSet<GitHubAccessToken> GitHubAccessTokens => Set<GitHubAccessToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Application);

        modelBuilder.ApplyConfiguration(new HabitConfiguration());
        modelBuilder.ApplyConfiguration(new TagConfiguration());
        modelBuilder.ApplyConfiguration(new HabitTagConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new GitHubAccessTokenConfiguration());
    }
}
