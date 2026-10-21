using KukaMiddleware.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KukaMiddleware.Api.Data.Configurations;

public class RobotConfiguration : IEntityTypeConfiguration<Robot>
{
    public void Configure(EntityTypeBuilder<Robot> builder)
    {
        builder.Property(r => r.KukaRobotId).HasMaxLength(64);
        builder.Property(r => r.Name).HasMaxLength(100);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(r => r.CurrentNode).HasMaxLength(64);
        builder.HasIndex(r => r.KukaRobotId).IsUnique();
    }
}
