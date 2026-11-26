using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Kuka;

// The one client for all KUKA FM calls (system-definition §4). Failures never throw: they come back
// as a result with an error that can be shown to the caller. Only caller cancellation throws.
public interface IKukaClient
{
    /// <summary>Vendor <c>submitMission</c> (§7.1.1); uses the mission's steps and <see cref="Mission.Robot"/>.</summary>
    Task<KukaResult> SubmitMissionAsync(Mission mission, CancellationToken ct);

    /// <summary>Vendor <c>missionCancel</c> (§7.1.2) with the configured cancel mode.</summary>
    Task<KukaResult> CancelMissionAsync(string missionCode, CancellationToken ct);

    /// <summary>Vendor <c>jobQuery</c> (§7.1.5) for one job code (= our mission code).</summary>
    Task<KukaResult<IReadOnlyList<KukaJob>>> QueryJobsAsync(string jobCode, CancellationToken ct);

    /// <summary>Vendor <c>robotQuery</c> (§7.3.2) for all robots.</summary>
    Task<KukaResult<IReadOnlyList<KukaRobot>>> QueryRobotsAsync(CancellationToken ct);
}

public record KukaResult(string? Error)
{
    public static readonly KukaResult Ok = new((string?)null);

    public bool Succeeded => Error is null;
}

public sealed record KukaResult<T>(T? Data, string? Error) : KukaResult(Error);

/// <summary>Used fields of a vendor job; <c>Status</c> is the numeric job status.</summary>
public sealed record KukaJob(string JobCode, string? RobotId, int Status);

/// <summary>Used fields of a vendor robot; <c>Status</c> is the numeric robot status.</summary>
public sealed record KukaRobot(string RobotId, int Status, double? BatteryLevel, string? NodeCode);
