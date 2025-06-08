using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Api.Database.Configurations;

public sealed class EntryImportJobConfiguration : IEntityTypeConfiguration<EntryImportJob>
{
    public void Configure(EntityTypeBuilder<EntryImportJob> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(500);

        builder.Property(x => x.UserId).HasMaxLength(500);

        builder.Property(x => x.FileName).HasMaxLength(500);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId);
    }
}
