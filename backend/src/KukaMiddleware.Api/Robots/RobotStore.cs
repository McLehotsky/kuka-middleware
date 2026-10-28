using KukaMiddleware.Api.Data;
using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Missions;
using Microsoft.EntityFrameworkCore;

namespace KukaMiddleware.Api.Robots;

public class RobotStore(AppDbContext db) : IRobotStore
{
    public Task<List<Robot>> ListAsync(CancellationToken ct) =>
        db.Set<Robot>().AsNoTracking().ToListAsync(ct);

    public Task<List<Mission>> ListActiveMissionsAsync(CancellationToken ct) =>
        db.Set<Mission>()
            .AsNoTracking()
            .Where(m => m.RobotId != null && MissionStatuses.NotFinal.Contains(m.Status))
            .ToListAsync(ct);
}
