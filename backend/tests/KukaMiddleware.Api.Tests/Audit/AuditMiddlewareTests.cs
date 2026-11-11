using System.Text.Json;
using KukaMiddleware.Api.Audit;
using KukaMiddleware.Api.Auth;
using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Tests.Missions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace KukaMiddleware.Api.Tests.Audit;

public class AuditMiddlewareTests
{
    private readonly IAuditWriter _writer = Substitute.For<IAuditWriter>();
    private readonly CurrentApiClient _current = new()
    {
        Client = new ApiClient { Id = 7, Name = "external-system", KeyHash = "hash", IsActive = true },
    };

    [Theory]
    [InlineData(201, true)]
    [InlineData(400, false)]
    [InlineData(500, false)]
    public async Task Create_attempt_records_client_status_time_and_correlation(int status, bool success)
    {
        AuditLog? row = null;
        _writer.WriteAsync(Arg.Do<AuditLog>(entry => row = entry), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var context = Context("/api/missions");
        context.TraceIdentifier = "request-42";
        var middleware = Middleware(ctx => { ctx.Response.StatusCode = status; return Task.CompletedTask; });

        await middleware.InvokeAsync(context, _current, _writer, TestData.Time());

        Assert.NotNull(row);
        Assert.Equal(7, row.ApiClientId);
        Assert.Equal(AuditAction.DispatchMission, row.Action);
        Assert.Equal(status, row.HttpStatus);
        Assert.Equal(success, row.Success);
        Assert.Equal(TestData.Now, row.Timestamp);
        Assert.Equal("request-42", row.CorrelationId);
        await _writer.Received(1).WriteAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(404, false)]
    [InlineData(409, false)]
    [InlineData(500, false)]
    public async Task Cancel_attempt_records_requested_mission(int status, bool success)
    {
        var id = Guid.NewGuid();
        AuditLog? row = null;
        string? requestedId = null;
        _writer.WriteAsync(Arg.Do<AuditLog>(entry =>
        {
            row = entry;
            requestedId = entry.Details!.RootElement.GetProperty("requestedMissionId").GetString();
        }), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var middleware = Middleware(ctx => { ctx.Response.StatusCode = status; return Task.CompletedTask; });

        await middleware.InvokeAsync(Context($"/api/missions/{id}/cancel"), _current, _writer, TestData.Time());

        Assert.Equal(AuditAction.CancelMission, row!.Action);
        Assert.Equal(success, row.Success);
        Assert.Equal(status is 200 or 409 ? (Guid?)id : null, row.MissionId);
        Assert.Equal(id.ToString(), requestedId);
    }

    [Fact]
    public async Task Created_mission_is_linked_from_location()
    {
        var id = Guid.NewGuid();
        var middleware = Middleware(ctx =>
        {
            ctx.Response.StatusCode = 201;
            ctx.Response.Headers.Location = $"/api/missions/{id}";
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(Context("/api/missions"), _current, _writer, TestData.Time());

        await _writer.Received(1).WriteAsync(Arg.Is<AuditLog>(row => row.MissionId == id), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("GET", "/api/missions")]
    [InlineData("GET", "/api/robots")]
    [InlineData("GET", "/api/health")]
    [InlineData("POST", "/api/unknown")]
    public async Task Non_operation_requests_are_not_audited(string method, string path)
    {
        var context = Context(path);
        context.Request.Method = method;

        await Middleware(_ => Task.CompletedTask).InvokeAsync(context, _current, _writer, TestData.Time());

        await _writer.DidNotReceiveWithAnyArgs().WriteAsync(default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Anonymous_rejection_is_not_written_to_database()
    {
        await Middleware(ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; })
            .InvokeAsync(Context("/api/missions"), new CurrentApiClient(), _writer, TestData.Time());

        await _writer.DidNotReceiveWithAnyArgs().WriteAsync(default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Response_is_not_sent_before_audit_is_persisted()
    {
        var context = Context("/api/missions");
        var originalBody = context.Response.Body;
        var middleware = Middleware(async ctx =>
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsync("validation error");
        });
        var persisted = false;
        _writer.WriteAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Assert.Equal(0, originalBody.Length);
            persisted = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, _current, _writer, TestData.Time());

        Assert.True(persisted);
        Assert.True(context.Response.Body.Length > 0);
    }

    private static AuditMiddleware Middleware(RequestDelegate next) => new(next, NullLogger<AuditMiddleware>.Instance);

    [Fact]
    public async Task Audit_failure_restores_body_and_does_not_send_success()
    {
        var context = Context("/api/missions");
        var originalBody = context.Response.Body;
        var failure = new InvalidOperationException("Audit store unavailable");
        _writer.WriteAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(failure));
        var middleware = Middleware(async ctx =>
        {
            ctx.Response.StatusCode = 201;
            await ctx.Response.WriteAsync("created");
        });

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, _current, _writer, TestData.Time()));

        Assert.Same(failure, actual);
        Assert.Same(originalBody, context.Response.Body);
        Assert.Equal(0, originalBody.Length);
    }

    [Fact]
    public async Task Operation_log_contains_result_and_actor_without_request_secrets()
    {
        var logger = new TestLogger<AuditMiddleware>();
        var context = Context("/api/missions");
        context.Request.Headers[ApiKeyMiddleware.HeaderName] = "private-api-key";
        var middleware = new AuditMiddleware(ctx => { ctx.Response.StatusCode = 400; return Task.CompletedTask; }, logger);

        await middleware.InvokeAsync(context, _current, _writer, TestData.Time());

        var entry = Assert.Single(logger.Entries);
        Assert.Contains(entry.Fields, field => field.Key == "ClientId" && Equals(field.Value, 7));
        Assert.Contains(entry.Fields, field => field.Key == "HttpStatus" && Equals(field.Value, 400));
        Assert.DoesNotContain("private-api-key", entry.Message);
        Assert.DoesNotContain(entry.Fields, field => field.Value?.ToString()?.Contains("private-api-key") == true);
    }

    private static DefaultHttpContext Context(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
