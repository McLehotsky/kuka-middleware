using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Robots;

public static class RobotStatuses
{
    /// <summary>Vendor robot status (§7.3.2) to robot state (system-definition §14 Phase 5 scope); null for an unknown code.</summary>
    public static RobotStatus? FromKuka(int kukaStatus) => kukaStatus switch
    {
        3 => RobotStatus.Available,
        1 or 4 or 5 or 6 => RobotStatus.Busy,
        2 or 7 => RobotStatus.Offline,
        _ => null,
    };
}
