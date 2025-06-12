using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Infrastructure.Persistence;
using UMS.Infrastructure.Settings;

namespace UMS.Infrastructure.BackgroundJobs
{
    public class CleanupOldRefreshTokensJob : BackgroundService
    {
        private readonly ILogger<CleanupOldRefreshTokensJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly CleanupSettings _cleanupSettings;

        public CleanupOldRefreshTokensJob(
            ILogger<CleanupOldRefreshTokensJob> logger, 
            IServiceScopeFactory scopeFactory, // Used to create a DI scope for DbContext
            IOptions<CleanupSettings> cleanupSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _cleanupSettings = cleanupSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Refresh Token Cleanup Job is starting.");

            // Wait a short period on startup before the first run.
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            // Using PeriodicTimer for a modern, non-overlapping time loop.
            using var timer = new PeriodicTimer(_cleanupSettings.Interval);

            while( await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Refresh Token Cleanup Job is running at: {time}", DateTimeOffset.Now);

                    // Create a new DI scope to resolve scoped services like ApplicationDbContext
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var cutoffDate = DateTime.UtcNow.Subtract(_cleanupSettings.TokenRetentionPeroid);

                    _logger.LogInformation("Purging refresh tokens older than {CutoffDate}", cutoffDate);

                    // Use ExecuteDeleteAsync for efficient bulk deletion
                    int deletedCount = await dbContext.RefreshTokens
                        .Where(rt => rt.ExpiresAtUtc < cutoffDate || (rt.RevokedAtUtc != null && rt.RevokedAtUtc < cutoffDate))
                        .ExecuteDeleteAsync(stoppingToken);

                    if(deletedCount > 0)
                    {
                        _logger.LogInformation("Successfully deleted {Count} old refresh tokens.", deletedCount);
                    }
                    else
                    {
                        _logger.LogInformation("No old refresh tokens found to delete.");
                    }
                }
                catch (OperationCanceledException)
                {
                    // This is expected when the application is shutting down.
                    _logger.LogInformation("Refresh Token Cleanup Job is stopping as cancellation was requested.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up old refresh tokens.");
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refresh Token Cleanup Job is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
