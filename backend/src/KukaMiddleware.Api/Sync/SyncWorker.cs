namespace KukaMiddleware.Api.Sync;

public interface ISyncJob
{
    Task RunAsync(CancellationToken ct);
}

// Runs one sync job at startup and then on every interval tick, each run in its own DI scope (own DbContext).
// A failing run is logged and retried on the next tick; the worker never stops the host.
public sealed class SyncWorker<TJob>(IServiceScopeFactory scopes, TimeSpan interval, TimeProvider time, ILogger<SyncWorker<TJob>> logger)
    : BackgroundService where TJob : ISyncJob
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval, time);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<TJob>().RunAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "{SyncJob} failed; retry in the next cycle", typeof(TJob).Name);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
