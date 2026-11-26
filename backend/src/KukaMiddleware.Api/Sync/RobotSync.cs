using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Kuka;
using KukaMiddleware.Api.Robots;

namespace KukaMiddleware.Api.Sync;

// UC7: insert new robots and update state, battery, node and times of known ones. A failed call changes nothing;
// robots missing from the response and robots with an unknown status stay unchanged.
public sealed class RobotSync(IKukaClient kuka, ISyncStore store, TimeProvider time, ILogger<RobotSync> logger) : ISyncJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var result = await kuka.QueryRobotsAsync(ct);
        if (!result.Succeeded)
        {
            logger.LogWarning("Robot sync skipped, data unchanged: {Error}", result.Error);
            return;
        }

        var robots = (await store.ListRobotsAsync(ct)).ToDictionary(r => r.KukaRobotId);
        var now = time.GetUtcNow();
        var changed = 0;
        foreach (var kukaRobot in result.Data!)
        {
            if (string.IsNullOrWhiteSpace(kukaRobot.RobotId))
            {
                logger.LogWarning("Robot sync ignored a KUKA FM robot without robotId");
                continue;
            }
            if (RobotStatuses.FromKuka(kukaRobot.Status) is not { } status)
            {
                logger.LogWarning("Robot sync: robot {KukaRobotId} has unknown KUKA status {KukaStatus}; robot unchanged",
                    kukaRobot.RobotId, kukaRobot.Status);
                continue;
            }

            if (!robots.TryGetValue(kukaRobot.RobotId, out var robot))
            {
                robot = new Robot { KukaRobotId = kukaRobot.RobotId };
                robots.Add(robot.KukaRobotId, robot);
                store.AddRobot(robot);
                changed++;
            }
            else if (robot.Status != status)
                changed++;
            // An Offline robot keeps its last known battery and node when KUKA FM reports none.
            var offline = status == RobotStatus.Offline;
            robot.Status = status;
            robot.BatteryLevel = Percent(kukaRobot.BatteryLevel) ?? (offline ? robot.BatteryLevel : null);
            robot.CurrentNode = kukaRobot.NodeCode ?? (offline ? robot.CurrentNode : null);
            robot.UpdatedAt = now;
            if (!offline)
                robot.LastSeenAt = now;
        }

        await store.SaveChangesAsync(ct);
        if (changed > 0)
            logger.LogInformation("Robot sync added or changed the status of {ChangedCount} robots", changed);
    }

    // The vendor documents batteryLevel as percent, but the lab KUKA FM reports 1.0 for idle robots:
    // values up to 1 are read as a fraction (system-definition §14 Phase 5 scope, battery scale).
    private static int? Percent(double? level) =>
        level is { } value ? (int)Math.Round(Math.Clamp(value <= 1 ? value * 100 : value, 0, 100)) : null;
}
