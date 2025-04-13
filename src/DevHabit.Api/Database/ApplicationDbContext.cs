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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Application);

        modelBuilder.ApplyConfiguration(new HabitConfiguration());
        modelBuilder.ApplyConfiguration(new TagConfiguration());
        modelBuilder.ApplyConfiguration(new HabitTagConfiguration());
    }
}
