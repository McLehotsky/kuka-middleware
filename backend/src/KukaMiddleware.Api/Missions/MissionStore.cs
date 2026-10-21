using KukaMiddleware.Api.Data;
using KukaMiddleware.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KukaMiddleware.Api.Missions;

public class MissionStore(AppDbContext db) : IMissionStore
{
    public Task<long> NextMissionCodeNumberAsync(CancellationToken ct) =>
        db.Database.SqlQuery<long>($"SELECT nextval({AppDbContext.MissionCodeSequence}::regclass) AS \"Value\"").SingleAsync(ct);

    public Task<bool> RobotExistsAsync(int robotId, CancellationToken ct) =>
        db.Set<Robot>().AnyAsync(r => r.Id == robotId, ct);

    public Task<bool> MissionExistsAsync(Guid id, CancellationToken ct) =>
        db.Set<Mission>().AnyAsync(m => m.Id == id, ct);

    public Task<Mission?> FindAsync(Guid id, CancellationToken ct) =>
        db.Set<Mission>()
            .Include(m => m.Robot)
            .Include(m => m.CreatedByClient)
            .Include(m => m.Steps)
            .SingleOrDefaultAsync(m => m.Id == id, ct);

    public Task<List<Mission>> ListAsync(IReadOnlyCollection<MissionStatus>? statuses, CancellationToken ct) =>
        db.Set<Mission>()
            .AsNoTracking()
            .Include(m => m.Robot)
            .Where(m => statuses == null || statuses.Contains(m.Status))
            .ToListAsync(ct);

    public Task<List<MissionStatusHistory>> ListHistoryAsync(Guid missionId, CancellationToken ct) =>
        db.Set<MissionStatusHistory>().AsNoTracking().Where(h => h.MissionId == missionId).ToListAsync(ct);

    public void Add(Mission mission) => db.Add(mission);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
