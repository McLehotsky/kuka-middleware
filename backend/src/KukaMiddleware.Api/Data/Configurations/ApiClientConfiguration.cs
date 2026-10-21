using KukaMiddleware.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KukaMiddleware.Api.Data.Configurations;

public class ApiClientConfiguration : IEntityTypeConfiguration<ApiClient>
{
    public void Configure(EntityTypeBuilder<ApiClient> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(100);
        builder.Property(c => c.KeyHash).HasMaxLength(128);
        builder.Property(c => c.Role).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(c => c.KeyHash).IsUnique();

        // Phase 6 has no authentication: all missions belong to this client (system-definition §14, Q-L).
        // The KeyHash is a sentinel that no real key hash can match; phase 8 replaces it with a real key.
        builder.HasData(new ApiClient
        {
            Id = 1,
            Name = "web-ui",
            KeyHash = "!disabled-seed-web-ui",
            Role = ApiClientRole.Operator,
            IsActive = true,
            CreatedAt = new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero),
        });
    }
}
