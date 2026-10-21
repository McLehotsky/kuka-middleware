using KukaMiddleware.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KukaMiddleware.Api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(32);
        builder.Property(a => a.Details).HasColumnType("jsonb");
        builder.Property(a => a.CorrelationId).HasMaxLength(64);
        builder.HasIndex(a => a.Timestamp).IsDescending();
        builder.HasIndex(a => a.MissionId);

        builder.HasOne(a => a.ApiClient).WithMany().HasForeignKey(a => a.ApiClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Mission).WithMany().HasForeignKey(a => a.MissionId).OnDelete(DeleteBehavior.SetNull);
    }
}
