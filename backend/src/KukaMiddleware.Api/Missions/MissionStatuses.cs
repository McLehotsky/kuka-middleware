using KukaMiddleware.Api.Data.Entities;
using static KukaMiddleware.Api.Data.Entities.MissionStatus;

namespace KukaMiddleware.Api.Missions;

// Status sets from system-definition §5.
public static class MissionStatuses
{
    public static readonly MissionStatus[] Final = [Completed, Cancelled, Failed];
    public static readonly MissionStatus[] NotFinal = [Created, Submitted, InProgress, Cancelling];
    public static readonly MissionStatus[] Cancellable = [Created, Submitted, InProgress];
}
