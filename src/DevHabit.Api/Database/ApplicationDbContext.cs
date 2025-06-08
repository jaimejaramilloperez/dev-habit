using DevHabit.Api.Database.Configurations;
using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Database;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<HabitTag> HabitTags => Set<HabitTag>();
    public DbSet<User> Users => Set<User>();
    public DbSet<GitHubAccessToken> GitHubAccessTokens => Set<GitHubAccessToken>();
    public DbSet<EntryImportJob> EntryImportJobs => Set<EntryImportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Application);

        modelBuilder.ApplyConfiguration(new HabitConfiguration());
        modelBuilder.ApplyConfiguration(new EntryConfiguration());
        modelBuilder.ApplyConfiguration(new TagConfiguration());
        modelBuilder.ApplyConfiguration(new HabitTagConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new GitHubAccessTokenConfiguration());
        modelBuilder.ApplyConfiguration(new EntryImportJobConfiguration());
    }
}
