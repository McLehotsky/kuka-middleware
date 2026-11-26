using KukaMiddleware.Api.Data;
using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Missions;
using Microsoft.EntityFrameworkCore;

namespace KukaMiddleware.Api.Sync;

public sealed class SyncStore(AppDbContext db) : ISyncStore
{
    public Task<List<Robot>> ListRobotsAsync(CancellationToken ct) => db.Set<Robot>().ToListAsync(ct);

    public Task<int?> FindRobotIdAsync(string kukaRobotId, CancellationToken ct) =>
        db.Set<Robot>().Where(r => r.KukaRobotId == kukaRobotId).Select(r => (int?)r.Id).SingleOrDefaultAsync(ct);

    public void AddRobot(Robot robot) => db.Add(robot);

    public Task<List<Mission>> ListActiveMissionsAsync(CancellationToken ct) =>
        db.Set<Mission>().Where(m => MissionStatuses.Active.Contains(m.Status)).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
