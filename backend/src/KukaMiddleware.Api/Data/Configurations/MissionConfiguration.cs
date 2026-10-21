using KukaMiddleware.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KukaMiddleware.Api.Data.Configurations;

public class MissionConfiguration : IEntityTypeConfiguration<Mission>
{
    public void Configure(EntityTypeBuilder<Mission> builder)
    {
        builder.Property(m => m.MissionCode).HasMaxLength(64);
        builder.Property(m => m.ExternalRef).HasMaxLength(100);
        builder.Property(m => m.MissionType).HasMaxLength(64);
        builder.Property(m => m.Priority).HasDefaultValue(2);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(m => m.ErrorMessage).HasMaxLength(1000);

        builder.HasIndex(m => m.MissionCode).IsUnique();
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.CreatedAt).IsDescending();

        builder.HasOne(m => m.Robot).WithMany().HasForeignKey(m => m.RobotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.CreatedByClient).WithMany().HasForeignKey(m => m.CreatedByClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(m => m.Steps).WithOne().HasForeignKey(s => s.MissionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(m => m.StatusHistory).WithOne().HasForeignKey(h => h.MissionId).OnDelete(DeleteBehavior.Cascade);
    }
}
