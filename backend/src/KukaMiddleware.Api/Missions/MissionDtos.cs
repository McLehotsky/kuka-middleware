using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Missions;

// Request fields are nullable strings so that missing or unknown values reach the validator
// and come back as a keyed ValidationProblem instead of a JSON binding error.
public record CreateMissionRequest(
    string? MissionType,
    int? Priority,
    int? RobotId,
    string? ExternalRef,
    IReadOnlyList<CreateMissionStep>? Steps);

public record CreateMissionStep(string? NodeCode, string? Action);

public record CreateMissionResponse(Guid Id, string MissionCode);

public record MissionSummary(
    Guid Id,
    string MissionCode,
    string? ExternalRef,
    string MissionType,
    int Priority,
    MissionStatus Status,
    string? RobotName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSyncedAt);

public record MissionDetail(
    Guid Id,
    string MissionCode,
    string? ExternalRef,
    string MissionType,
    int Priority,
    MissionStatus Status,
    string? RobotName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSyncedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? FinishedAt,
    string? ErrorMessage,
    string CreatedBy,
    IReadOnlyList<MissionStepDto> Steps);

public record MissionStepDto(int Sequence, string NodeCode, string Action);

public record MissionHistoryEntry(
    MissionStatus? PreviousStatus,
    MissionStatus Status,
    StatusSource Source,
    string? Message,
    DateTimeOffset ChangedAt);
