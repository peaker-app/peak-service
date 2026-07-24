using Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeakService.Domain.MountainRanges;

namespace PeakService.Infrastructure.Persistence.Configurations;

internal sealed class MountainRangeConfiguration : EntityConfiguration<MountainRange>
{
    public override void Configure(EntityTypeBuilder<MountainRange> builder)
    {
        base.Configure(builder);

        builder.ToTable("mountain_ranges");

        builder.Property(range => range.Name)
            .HasColumnName("name")
            .HasMaxLength(MountainRange.MaxNameLength)
            .IsRequired();

        builder.Property(range => range.ParentRangeId).HasColumnName("parent_range_id");

        builder.HasOne<MountainRange>()
            .WithMany()
            .HasForeignKey(range => range.ParentRangeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
