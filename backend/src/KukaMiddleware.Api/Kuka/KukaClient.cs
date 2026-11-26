using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using KukaMiddleware.Api.Data.Entities;
using Microsoft.Extensions.Options;

namespace KukaMiddleware.Api.Kuka;

// Typed HttpClient for KUKA FM (vendor reference chapter 7). Every call: POST {BaseUrl}/interfaces/api/amr/{api}
// with the raw token as Authorization. A 401 is logged as an authentication error and fails without a retry,
// since there is no token endpoint yet (system-definition §14 Phase 5 scope, deviation from backlog 3.1).
// Logs carry the API name, outcome and duration; never the token, headers or bodies.
public sealed class KukaClient : IKukaClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly IKukaTokenProvider _tokens;
    private readonly KukaFmOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<KukaClient> _logger;

    public KukaClient(HttpClient http, IKukaTokenProvider tokens, IOptions<KukaFmOptions> options, TimeProvider time, ILogger<KukaClient> logger)
    {
        _options = options.Value;
        http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/interfaces/api/amr/");
        http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        (_http, _tokens, _time, _logger) = (http, tokens, time, logger);
    }

    public async Task<KukaResult> SubmitMissionAsync(Mission mission, CancellationToken ct) =>
        await SendAsync<JsonElement?>("submitMission", new SubmitMissionRequest(
            _options.OrgId,
            NewRequestId(),
            mission.MissionCode,
            mission.MissionType,
            mission.Robot is { } robot ? [robot.KukaRobotId] : null,
            mission.Priority,
            [.. mission.Steps.OrderBy(s => s.Sequence).Select(ToMissionData)]), ct);

    public async Task<KukaResult> CancelMissionAsync(string missionCode, CancellationToken ct) =>
        await SendAsync<JsonElement?>("missionCancel", new CancelMissionRequest(NewRequestId(), missionCode, _options.CancelMode), ct);

    public async Task<KukaResult<IReadOnlyList<KukaJob>>> QueryJobsAsync(string jobCode, CancellationToken ct) =>
        OrEmpty(await SendAsync<IReadOnlyList<KukaJob>>("jobQuery", new JobQueryRequest(jobCode), ct));

    public async Task<KukaResult<IReadOnlyList<KukaRobot>>> QueryRobotsAsync(CancellationToken ct) =>
        OrEmpty(await SendAsync<IReadOnlyList<KukaRobot>>("robotQuery", new { }, ct));

    // Mapping decided in system-definition §14 Phase 5 scope (RACK_MOVE only).
    private MissionDataItem ToMissionData(MissionStep step) => step.Action switch
    {
        "Load" => new(step.Sequence, step.NodeCode, "NODE_POINT", "UP_CONTAINER", "AUTO", 0),
        "Unload" => new(step.Sequence, step.NodeCode, "NODE_POINT", "DOWN_CONTAINER", "AUTO", 0),
        "Wait" => new(step.Sequence, step.NodeCode, "NODE_POINT", null, "AUTO", _options.WaitMillis),
        _ => throw new InvalidOperationException($"Unknown step action '{step.Action}'."),
    };

    private static string NewRequestId() => Guid.NewGuid().ToString("N");

    private static KukaResult<IReadOnlyList<T>> OrEmpty<T>(KukaResult<IReadOnlyList<T>> result) =>
        result.Succeeded && result.Data is null ? result with { Data = [] } : result;

    private async Task<KukaResult<T>> SendAsync<T>(string api, object body, CancellationToken ct)
    {
        var started = _time.GetTimestamp();
        using var request = new HttpRequestMessage(HttpMethod.Post, api)
        {
            Content = JsonContent.Create(body, body.GetType(), options: Json),
        };
        if (await _tokens.GetTokenAsync(ct) is { } token)
            request.Headers.TryAddWithoutValidation("Authorization", token);

        try
        {
            using var response = await _http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return Failure<T>(api, "AuthenticationError", "KUKA FM authentication failed (HTTP 401).", started, LogLevel.Error);
            if (!response.IsSuccessStatusCode)
                return Failure<T>(api, "HttpError", $"KUKA FM returned HTTP {(int)response.StatusCode}.", started);

            var envelope = await response.Content.ReadFromJsonAsync<KukaEnvelope<T>>(Json, ct);
            if (envelope is null)
                return Failure<T>(api, "InvalidResponse", "KUKA FM returned an invalid response.", started);
            if (!envelope.Success)
                return Failure<T>(api, "Rejected", string.IsNullOrWhiteSpace(envelope.Message)
                    ? $"KUKA FM rejected the request (code {envelope.Code})."
                    : $"KUKA FM rejected the request (code {envelope.Code}): {envelope.Message}", started);

            _logger.LogDebug("KUKA FM {ApiName} {Outcome} in {ElapsedMs} ms", api, "Succeeded", Elapsed(started));
            return new KukaResult<T>(envelope.Data, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Failure<T>(api, "Timeout", $"KUKA FM did not respond within {_options.TimeoutSeconds} s.", started);
        }
        catch (HttpRequestException ex)
        {
            return Failure<T>(api, "Unreachable", "KUKA FM is unreachable.", started, exception: ex);
        }
        catch (JsonException)
        {
            return Failure<T>(api, "InvalidResponse", "KUKA FM returned an invalid response.", started);
        }
    }

    private KukaResult<T> Failure<T>(string api, string outcome, string error, long started,
        LogLevel level = LogLevel.Warning, Exception? exception = null)
    {
        _logger.Log(level, exception, "KUKA FM {ApiName} {Outcome} in {ElapsedMs} ms: {Error}", api, outcome, Elapsed(started), error);
        return new KukaResult<T>(default, error);
    }

    private long Elapsed(long started) => (long)_time.GetElapsedTime(started).TotalMilliseconds;

    private sealed record KukaEnvelope<T>(T? Data, string? Code, string? Message, bool Success);

    private sealed record SubmitMissionRequest(
        string OrgId,
        string RequestId,
        string MissionCode,
        string MissionType,
        IReadOnlyList<string>? RobotIds,
        int Priority,
        IReadOnlyList<MissionDataItem> MissionData);

    private sealed record MissionDataItem(int Sequence, string Position, string Type, string? ActionType, string PassStrategy, int WaitingMillis);

    private sealed record CancelMissionRequest(string RequestId, string MissionCode, string CancelMode);

    private sealed record JobQueryRequest(string JobCode);
}
