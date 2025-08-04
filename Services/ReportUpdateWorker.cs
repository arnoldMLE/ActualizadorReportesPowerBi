using Microsoft.Extensions.Options;
using PowerBiUpdater.Models;
using PowerBiUpdater.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerBiUpdater.Services
{
    public class ReportUpdateWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ScheduleConfig _scheduleConfig;
        private readonly ILogger<ReportUpdateWorker> _logger;

        public ReportUpdateWorker(IServiceProvider serviceProvider, IOptions<ScheduleConfig> scheduleConfig,
            ILogger<ReportUpdateWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _scheduleConfig = scheduleConfig.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Report Update Worker started");

            if (_scheduleConfig.RunOnStartup)
            {
                await ExecuteUpdateAsync();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nextRun = GetNextRunTime();
                    var delay = nextRun - DateTime.Now;

                    if (delay > TimeSpan.Zero)
                    {
                        _logger.LogInformation("Next update scheduled for {NextRun} (in {Delay})",
                            nextRun, delay);
                        await Task.Delay(delay, stoppingToken);
                    }

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await ExecuteUpdateAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Report Update Worker cancellation requested");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Report Update Worker");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait before retrying
                }
            }

            _logger.LogInformation("Report Update Worker stopped");
        }

        private async Task ExecuteUpdateAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var updateService = scope.ServiceProvider.GetRequiredService<IReportUpdateService>();

            try
            {
                await updateService.ExecuteUpdateCycleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during report update execution");
            }
        }

        private DateTime GetNextRunTime()
        {
            var now = DateTime.Now;

            // If specific run times are configured, use those
            if (_scheduleConfig.RunTimes.Any())
            {
                var today = now.Date;
                var scheduledTimes = _scheduleConfig.RunTimes
                    .Select(time => DateTime.TryParse($"{today:yyyy-MM-dd} {time}", out var dt) ? dt : (DateTime?)null)
                    .Where(dt => dt.HasValue && dt > now)
                    .Select(dt => dt!.Value)
                    .OrderBy(dt => dt)
                    .ToList();

                if (scheduledTimes.Any())
                {
                    return scheduledTimes.First();
                }

                // No more runs today, get first run tomorrow
                var tomorrow = today.AddDays(1);
                var firstRunTomorrow = _scheduleConfig.RunTimes
                    .Select(time => DateTime.TryParse($"{tomorrow:yyyy-MM-dd} {time}", out var dt) ? dt : (DateTime?)null)
                    .Where(dt => dt.HasValue)
                    .Select(dt => dt!.Value)
                    .OrderBy(dt => dt)
                    .FirstOrDefault();

                return firstRunTomorrow != default ? firstRunTomorrow : now.AddMinutes(_scheduleConfig.IntervalMinutes);
            }

            // Use interval-based scheduling
            return now.AddMinutes(_scheduleConfig.IntervalMinutes);
        }
    }
}
