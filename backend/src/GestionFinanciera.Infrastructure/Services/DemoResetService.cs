using GestionFinanciera.Infrastructure.Identity;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GestionFinanciera.Infrastructure.Services;

/// <summary>
/// Periodically resets the demo company dataset (transactions, categories and
/// audit trail) so the public demo never accumulates garbage. Runs in-process
/// with a PeriodicTimer — no HTTP endpoint, same idea as NestJS cron-jobs.
///
/// Safety: only ever touches the demo company; idempotent by design, so a
/// double trigger (e.g. multiple App Service instances) is harmless.
/// No-op when Demo:Enabled=false.
/// </summary>
public sealed class DemoResetService(
    IServiceScopeFactory scopeFactory,
    IOptions<DemoOptions> demoOptions,
    ILogger<DemoResetService> logger) : BackgroundService
{
    private readonly DemoOptions _options = demoOptions.Value;

    /// <summary>
    /// Interval between resets; clamped to a minimum of 1 hour so a bad config
    /// (0, negative) can never tight-loop the service.
    /// </summary>
    public static TimeSpan GetInterval(DemoOptions options) =>
        TimeSpan.FromHours(Math.Max(1, options.ResetIntervalHours));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = GetInterval(_options);

        logger.LogInformation("Demo reset scheduled every {Hours}h (Enabled={Enabled}).",
            interval.TotalHours, _options.Enabled);

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // DemoSeeder is scoped (DbContext, UserManager) — resolve it
                // inside a scope created from the hosted-service singleton.
                using IServiceScope scope = scopeFactory.CreateScope();
                var seeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();
                await seeder.ResetDemoDataAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // graceful shutdown — stop the loop
            }
            catch (Exception ex)
            {
                // Never kill the host: log and retry on the next interval.
                logger.LogError(ex, "Demo data reset failed — will retry next interval.");
            }
        }

        logger.LogInformation("Demo reset service stopped.");
    }
}
