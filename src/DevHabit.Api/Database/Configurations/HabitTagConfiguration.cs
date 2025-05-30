using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Api.Database.Configurations;

public sealed class HabitTagConfiguration : IEntityTypeConfiguration<HabitTag>
{
    public void Configure(EntityTypeBuilder<HabitTag> builder)
    {
        builder.HasKey(x => new { x.HabitId, x.TagId });

        builder.HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId);

        builder.HasOne(x => x.Habit)
            .WithMany(x => x.HabitTags)
            .HasForeignKey(x => x.HabitId);
    }
}
