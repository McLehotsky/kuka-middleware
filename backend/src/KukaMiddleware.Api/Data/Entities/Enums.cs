namespace KukaMiddleware.Api.Data.Entities;

public enum ApiClientRole
{
    Operator,
    ExternalSystem,
}

public enum RobotStatus
{
    Available,
    Busy,
    Offline,
}

public enum MissionStatus
{
    Created,
    Submitted,
    InProgress,
    Cancelling,
    Completed,
    Cancelled,
    Failed,
}

public enum StatusSource
{
    Api,
    Sync,
}

public enum AuditAction
{
    DispatchMission,
    CancelMission,
}
