using System.Net;
using System.Text.Json;
using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Kuka;
using KukaMiddleware.Api.Tests.Missions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KukaMiddleware.Api.Tests.Kuka;

public class KukaClientTests
{
    private const string Token = "secret-token-123";
    private const string Ok = """{"data":null,"code":"0","message":null,"success":true}""";

    private readonly TestLogger<KukaClient> _logger = new();
    private KukaFmOptions _options = new()
    {
        BaseUrl = "https://kukafm.test",
        AccessToken = Token,
        OrgId = "UNIVERSAL",
        CancelMode = "FORCE",
        TimeoutSeconds = 10,
        WaitMillis = 3000,
    };

    private KukaClient Client(StubHandler handler)
    {
        var options = Options.Create(_options);
        return new KukaClient(new HttpClient(handler), new StaticKukaTokenProvider(options), options, TimeProvider.System, _logger);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Every operation of the client, so that transport behaviour is checked for all four.
    public static TheoryData<string> Apis => ["submitMission", "missionCancel", "jobQuery", "robotQuery"];

    private static async Task<KukaResult> Call(KukaClient client, string api) => api switch
    {
        "submitMission" => await client.SubmitMissionAsync(RackMission(), Ct),
        "missionCancel" => await client.CancelMissionAsync("M-2026-0001", Ct),
        "jobQuery" => await client.QueryJobsAsync("M-2026-0001", Ct),
        _ => await client.QueryRobotsAsync(Ct),
    };

    private static Mission RackMission(Robot? robot = null)
    {
        var mission = TestData.Mission(MissionStatus.Created);
        mission.MissionCode = "M-2026-0007";
        mission.Priority = 3;
        mission.Robot = robot;
        // Deliberately out of order: the client sends steps by sequence.
        mission.Steps.Add(new MissionStep { Sequence = 3, NodeCode = "N-03", Action = "Unload" });
        mission.Steps.Add(new MissionStep { Sequence = 1, NodeCode = "N-01", Action = "Load" });
        mission.Steps.Add(new MissionStep { Sequence = 2, NodeCode = "N-02", Action = "Wait" });
        return mission;
    }

    private static JsonElement Body(StubHandler handler) => JsonDocument.Parse(Assert.Single(handler.Requests).Body).RootElement;

    // --- transport (backlog 3.1) ---

    [Theory]
    [MemberData(nameof(Apis))]
    public async Task Posts_to_the_vendor_api_path(string api)
    {
        var handler = StubHandler.Json(Ok);

        await Call(Client(handler), api);

        var request = Assert.Single(handler.Requests).Request;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"https://kukafm.test/interfaces/api/amr/{api}", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task Base_url_with_trailing_slash_gives_the_same_path()
    {
        _options = _options with { BaseUrl = "https://kukafm.test/" };
        var handler = StubHandler.Json(Ok);

        await Client(handler).QueryRobotsAsync(Ct);

        Assert.Equal("https://kukafm.test/interfaces/api/amr/robotQuery", handler.Requests[0].Request.RequestUri!.ToString());
    }

    [Theory]
    [MemberData(nameof(Apis))]
    public async Task Every_call_sends_the_raw_token_as_authorization(string api)
    {
        var handler = StubHandler.Json(Ok);

        await Call(Client(handler), api);

        var request = handler.Requests[0].Request;
        Assert.True(request.Headers.TryGetValues("Authorization", out var values));
        Assert.Equal([Token], values);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Missing_token_sends_the_call_without_authorization(string? token)
    {
        _options = _options with { AccessToken = token };
        var handler = StubHandler.Json(Ok);

        var result = await Client(handler).QueryRobotsAsync(Ct);

        Assert.True(result.Succeeded);
        Assert.False(handler.Requests[0].Request.Headers.Contains("Authorization"));
    }

    [Fact]
    public async Task Failed_call_is_logged_as_warning()
    {
        await Client(StubHandler.Json(Ok, HttpStatusCode.InternalServerError)).QueryRobotsAsync(Ct);

        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Unauthorized_fails_without_retry()
    {
        var handler = StubHandler.Json("", HttpStatusCode.Unauthorized);

        var result = await Client(handler).QueryRobotsAsync(Ct);

        Assert.False(result.Succeeded);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Unauthorized_is_logged_as_authentication_error()
    {
        await Client(StubHandler.Json("", HttpStatusCode.Unauthorized)).QueryRobotsAsync(Ct);

        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Non_success_status_is_a_failure(HttpStatusCode status)
    {
        var result = await Client(StubHandler.Json(Ok, status)).SubmitMissionAsync(RackMission(), Ct);

        Assert.False(result.Succeeded);
        Assert.Contains(((int)status).ToString(), result.Error);
    }

    [Fact]
    public async Task Vendor_rejection_is_a_failure_with_code_and_message()
    {
        var handler = StubHandler.Json("""{"data":null,"code":"100002","message":"mission code exists","success":false}""");

        var result = await Client(handler).SubmitMissionAsync(RackMission(), Ct);

        Assert.False(result.Succeeded);
        Assert.Contains("100002", result.Error);
        Assert.Contains("mission code exists", result.Error);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    public async Task Vendor_rejection_without_message_has_no_trailing_separator(string message)
    {
        var handler = StubHandler.Json($$"""{"data":null,"code":"100002","message":{{message}},"success":false}""");

        var result = await Client(handler).SubmitMissionAsync(RackMission(), Ct);

        Assert.Equal("KUKA FM rejected the request (code 100002).", result.Error);
    }

    [Fact]
    public async Task Timeout_is_a_failure()
    {
        var handler = StubHandler.Throws(new TaskCanceledException("timeout", new TimeoutException()));

        var result = await Client(handler).CancelMissionAsync("M-2026-0001", Ct);

        Assert.False(result.Succeeded);
        Assert.Contains("did not respond", result.Error);
    }

    [Fact]
    public async Task Unreachable_host_is_a_failure()
    {
        var result = await Client(StubHandler.Throws(new HttpRequestException("No route to host"))).QueryRobotsAsync(Ct);

        Assert.False(result.Succeeded);
        Assert.Contains("unreachable", result.Error);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("null")]
    public async Task Invalid_response_body_is_a_failure(string body)
    {
        var result = await Client(StubHandler.Json(body)).QueryRobotsAsync(Ct);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_turned_into_a_failure()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Client(StubHandler.Json(Ok)).QueryRobotsAsync(cts.Token));
    }

    [Theory]
    [MemberData(nameof(Apis))]
    public async Task Successful_call_is_logged_at_debug_with_api_name_and_outcome(string api)
    {
        await Call(Client(StubHandler.Json(Ok)), api);

        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Debug &&
            e.Fields.Any(f => f.Key == "ApiName" && Equals(f.Value, api)) &&
            e.Fields.Any(f => f.Key == "Outcome") &&
            e.Fields.Any(f => f.Key == "ElapsedMs"));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Logs_never_contain_the_token(HttpStatusCode status)
    {
        await Client(StubHandler.Json(Ok, status)).SubmitMissionAsync(RackMission(), Ct);

        Assert.NotEmpty(_logger.Entries);
        Assert.DoesNotContain(_logger.Entries, e =>
            e.Message.Contains(Token) || e.Fields.Any(f => f.Value?.ToString()?.Contains(Token) == true));
    }

    // --- payloads (vendor §7.1.1, §7.1.2, §7.1.5) ---

    [Fact]
    public async Task Submit_sends_org_mission_code_type_and_priority()
    {
        var handler = StubHandler.Json(Ok);

        await Client(handler).SubmitMissionAsync(RackMission(), Ct);

        var body = Body(handler);
        Assert.Equal("UNIVERSAL", body.GetProperty("orgId").GetString());
        Assert.Equal("M-2026-0007", body.GetProperty("missionCode").GetString());
        Assert.Equal("RACK_MOVE", body.GetProperty("missionType").GetString());
        Assert.Equal(3, body.GetProperty("priority").GetInt32());
    }

    [Theory]
    [InlineData("submitMission")]
    [InlineData("missionCancel")]
    public async Task Request_id_is_a_new_32_hex_value_per_call(string api)
    {
        var handler = StubHandler.Json(Ok);
        var client = Client(handler);

        await Call(client, api);
        await Call(client, api);

        var ids = handler.Requests.Select(r => JsonDocument.Parse(r.Body).RootElement.GetProperty("requestId").GetString()!).ToList();
        Assert.All(ids, id => Assert.Matches("^[0-9a-f]{32}$", id));
        Assert.NotEqual(ids[0], ids[1]);
    }

    [Fact]
    public async Task Submit_maps_steps_in_sequence_order_to_mission_data()
    {
        var handler = StubHandler.Json(Ok);

        await Client(handler).SubmitMissionAsync(RackMission(), Ct);

        var data = Body(handler).GetProperty("missionData").EnumerateArray().ToList();
        Assert.Equal([1, 2, 3], data.Select(d => d.GetProperty("sequence").GetInt32()));
        Assert.Equal(["N-01", "N-02", "N-03"], data.Select(d => d.GetProperty("position").GetString()));
        Assert.All(data, d => Assert.Equal("NODE_POINT", d.GetProperty("type").GetString()));
        Assert.All(data, d => Assert.Equal("AUTO", d.GetProperty("passStrategy").GetString()));
    }

    [Theory]
    [InlineData(0, "UP_CONTAINER", 0)]
    [InlineData(1, null, 3000)]
    [InlineData(2, "DOWN_CONTAINER", 0)]
    public async Task Submit_maps_step_action_to_container_action_and_waiting_time(int index, string? actionType, int waitingMillis)
    {
        var handler = StubHandler.Json(Ok);

        await Client(handler).SubmitMissionAsync(RackMission(), Ct);

        var step = Body(handler).GetProperty("missionData")[index];
        Assert.Equal(actionType, step.TryGetProperty("actionType", out var a) ? a.GetString() : null);
        Assert.Equal(waitingMillis, step.GetProperty("waitingMillis").GetInt32());
    }

    [Fact]
    public async Task Submit_sends_the_chosen_robot()
    {
        var handler = StubHandler.Json(Ok);
        var robot = new Robot { Id = 4, KukaRobotId = "14" };

        await Client(handler).SubmitMissionAsync(RackMission(robot), Ct);

        Assert.Equal(["14"], Body(handler).GetProperty("robotIds").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Submit_without_robot_lets_kuka_decide()
    {
        var handler = StubHandler.Json(Ok);

        await Client(handler).SubmitMissionAsync(RackMission(), Ct);

        Assert.False(Body(handler).TryGetProperty("robotIds", out _));
    }

    [Fact]
    public async Task Cancel_sends_mission_code_and_configured_mode()
    {
        _options = _options with { CancelMode = "NORMAL" };
        var handler = StubHandler.Json(Ok);

        await Client(handler).CancelMissionAsync("M-2026-0009", Ct);

        var body = Body(handler);
        Assert.Equal("M-2026-0009", body.GetProperty("missionCode").GetString());
        Assert.Equal("NORMAL", body.GetProperty("cancelMode").GetString());
    }

    [Fact]
    public async Task Job_query_filters_by_job_code()
    {
        var handler = StubHandler.Json("""{"data":[],"code":null,"message":null,"success":true}""");

        await Client(handler).QueryJobsAsync("M-2026-0009", Ct);

        Assert.Equal("M-2026-0009", Body(handler).GetProperty("jobCode").GetString());
    }

    [Fact]
    public async Task Job_query_returns_jobs()
    {
        var handler = StubHandler.Json("""
            {"data":[{"jobCode":"M-2026-0009","workflowId":100218,"robotId":"1","status":20,"warnCode":null}],
             "code":"0","message":null,"success":true}
            """);

        var result = await Client(handler).QueryJobsAsync("M-2026-0009", Ct);

        var job = Assert.Single(result.Data!);
        Assert.Equal(new KukaJob("M-2026-0009", "1", 20), job);
    }

    [Fact]
    public async Task Job_query_with_null_data_returns_no_jobs()
    {
        var result = await Client(StubHandler.Json("""{"data":null,"code":null,"message":null,"success":true}"""))
            .QueryJobsAsync("M-2026-0009", Ct);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task Robot_query_sends_an_empty_filter()
    {
        var handler = StubHandler.Json("""{"data":[],"code":"0","message":null,"success":true}""");

        await Client(handler).QueryRobotsAsync(Ct);

        Assert.Equal("{}", handler.Requests[0].Body);
    }

    [Fact]
    public async Task Robot_query_parses_the_live_response_shape()
    {
        // Shape observed on the lab KUKA FM on 2026-11-26.
        var handler = StubHandler.Json("""
            {"data":[{"robotId":"1","robotType":"KMP 1500P-EU-D diffDrive","mapCode":"Labak","floorNumber":"CHTN",
              "buildingCode":"Chrop_TN","containerCode":"","status":3,"occupyStatus":0,"batteryLevel":1.0,
              "nodeCode":"Labak-CHTN-15","nodeLabel":"15","nodeNumber":15,"x":"12505.0","missionCode":null}],
             "code":"0","message":null,"success":true}
            """);

        var result = await Client(handler).QueryRobotsAsync(Ct);

        Assert.Equal(new KukaRobot("1", 3, 1.0, "Labak-CHTN-15"), Assert.Single(result.Data!));
    }
}
