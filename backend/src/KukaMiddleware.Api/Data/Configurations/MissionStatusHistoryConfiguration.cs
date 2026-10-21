using KukaMiddleware.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KukaMiddleware.Api.Data.Configurations;

public class MissionStatusHistoryConfiguration : IEntityTypeConfiguration<MissionStatusHistory>
{
    public void Configure(EntityTypeBuilder<MissionStatusHistory> builder)
    {
        builder.Property(h => h.PreviousStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(h => h.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(h => h.Source).HasConversion<string>().HasMaxLength(32);
        builder.Property(h => h.Message).HasMaxLength(1000);
        builder.HasIndex(h => new { h.MissionId, h.ChangedAt });
    }
}
