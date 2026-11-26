using KukaMiddleware.Api.Data;
using KukaMiddleware.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KukaMiddleware.Api.Tests.Data;

public class MissionModelTests
{
    // Q11: PostgreSQL xmin is the row version of a mission, so concurrent cancel / sync writes conflict.
    [Fact]
    public void Mission_uses_xmin_as_concurrency_token()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=unused")
            .UseSnakeCaseNamingConvention()
            .Options;
        using var db = new AppDbContext(options);

        var property = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Mission))!.FindProperty(nameof(Mission.Version))!;

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal("xmin", property.GetColumnName());
    }
}
