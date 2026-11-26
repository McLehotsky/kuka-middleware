using System.Text.Json.Serialization;
using KukaMiddleware.Api;
using KukaMiddleware.Api.Auth;
using KukaMiddleware.Api.Audit;
using KukaMiddleware.Api.Data;
using KukaMiddleware.Api.Kuka;
using KukaMiddleware.Api.Missions;
using KukaMiddleware.Api.Robots;
using KukaMiddleware.Api.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());
var keys = new ApiKeySettings(builder.Configuration["ApiKeys:WebUi"]!, builder.Configuration["ApiKeys:ExternalSystem"]!);

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CurrentApiClient>();
builder.Services.AddScoped<IApiClientStore, ApiClientStore>();
builder.Services.AddSingleton<IAuditWriter, AuditWriter>();
builder.Services.AddScoped<IMissionStore, MissionStore>();
builder.Services.AddScoped<IMissionService, MissionService>();
builder.Services.AddScoped<IRobotStore, RobotStore>();
builder.Services.AddScoped<IRobotService, RobotService>();

builder.Services.AddOptions<KukaFmOptions>()
    .Bind(builder.Configuration.GetSection(KukaFmOptions.Section))
    .Validate(o => o.IsValid(), "KukaFm needs an absolute http(s) BaseUrl, an OrgId, a known CancelMode, " +
        "TimeoutSeconds 1-300 and WaitMillis >= 0.")
    .ValidateOnStart();
builder.Services.AddSingleton<IKukaTokenProvider, StaticKukaTokenProvider>();
builder.Services.AddHttpClient<IKukaClient, KukaClient>();

builder.Services.AddScoped<ISyncStore, SyncStore>();
builder.Services.AddScoped<RobotSync>();
builder.Services.AddScoped<MissionSync>();
var robotInterval = SyncInterval("Sync:RobotIntervalSeconds");
var missionInterval = SyncInterval("Sync:MissionIntervalSeconds");
builder.Services.AddHostedService(sp => ActivatorUtilities.CreateInstance<SyncWorker<RobotSync>>(sp, robotInterval));
builder.Services.AddHostedService(sp => ActivatorUtilities.CreateInstance<SyncWorker<MissionSync>>(sp, missionInterval));

var app = builder.Build();

if (string.IsNullOrWhiteSpace(app.Services.GetRequiredService<IOptions<KukaFmOptions>>().Value.AccessToken))
    app.Logger.LogWarning("KukaFm:AccessToken is not configured; KUKA FM calls are sent without Authorization");

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    await scope.ServiceProvider.GetRequiredService<IApiClientStore>().ConfigureAsync(keys, app.Lifetime.ApplicationStopping);
}

// The outer handler covers audit persistence errors before a buffered response is sent.
app.UseExceptionHandler();
app.UseMiddleware<AuditMiddleware>();
// Resolve endpoint errors (including malformed JSON) before the audit records the final status.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapHealthChecks("/api/health");
app.MapMissionEndpoints();
app.MapRobotEndpoints();

app.Run();

TimeSpan SyncInterval(string key) =>
    builder.Configuration.GetValue<int>(key) is var seconds and > 0
        ? TimeSpan.FromSeconds(seconds)
        : throw new InvalidOperationException($"{key} must be a positive number of seconds.");
