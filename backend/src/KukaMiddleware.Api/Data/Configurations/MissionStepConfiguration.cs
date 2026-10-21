using KukaMiddleware.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KukaMiddleware.Api.Data.Configurations;

public class MissionStepConfiguration : IEntityTypeConfiguration<MissionStep>
{
    public void Configure(EntityTypeBuilder<MissionStep> builder)
    {
        builder.Property(s => s.NodeCode).HasMaxLength(64);
        builder.Property(s => s.Action).HasMaxLength(64);
        builder.HasIndex(s => new { s.MissionId, s.Sequence }).IsUnique();
    }
}
