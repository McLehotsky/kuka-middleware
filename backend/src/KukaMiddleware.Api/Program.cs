using System.Text.Json.Serialization;
using KukaMiddleware.Api;
using KukaMiddleware.Api.Auth;
using KukaMiddleware.Api.Audit;
using KukaMiddleware.Api.Data;
using KukaMiddleware.Api.Missions;
using KukaMiddleware.Api.Robots;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
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

var app = builder.Build();

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
