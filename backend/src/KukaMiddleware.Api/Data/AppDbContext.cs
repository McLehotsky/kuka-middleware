using Microsoft.EntityFrameworkCore;

namespace KukaMiddleware.Api.Data;

// No DbSet properties on purpose: table names then come from the entity type names (snake_case,
// singular, e.g. mission_status_history) and FK constraint names stay consistent. Use Set<T>().
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Number part of Mission.MissionCode (M-{yyyy}-{NNNN}); never reset (system-definition §8, Q-A).
    public const string MissionCodeSequence = "mission_code_seq";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(MissionCodeSequence);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
