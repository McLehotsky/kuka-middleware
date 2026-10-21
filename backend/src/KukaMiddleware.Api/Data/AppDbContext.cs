using Microsoft.EntityFrameworkCore;

namespace KukaMiddleware.Api.Data;

// No DbSet properties on purpose: table names then come from the entity type names (snake_case,
// singular, e.g. mission_status_history) and FK constraint names stay consistent. Use Set<T>().
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
