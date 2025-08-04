using Microsoft.Extensions.Options;
using PowerBiUpdater.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerBiUpdater.Services
{
    public interface IReportUpdateService
    {
        Task ExecuteUpdateCycleAsync();
    }
    public class ReportUpdateService : IReportUpdateService
    {
        private readonly IPowerBIService _powerBIService;
        private readonly PowerBIConfig _config;
        private readonly ILogger<ReportUpdateService> _logger;

        public ReportUpdateService(IPowerBIService powerBIService, IOptions<PowerBIConfig> config,
            ILogger<ReportUpdateService> logger)
        {
            _powerBIService = powerBIService;
            _config = config.Value;
            _logger = logger;
        }

        public async Task ExecuteUpdateCycleAsync()
        {
            _logger.LogInformation("Starting report update cycle for {DatasetCount} datasets", _config.Datasets.Count);

            var tasks = _config.Datasets.Select(ProcessDatasetWithRetry);
            await Task.WhenAll(tasks);

            _logger.LogInformation("Completed report update cycle");
        }

        private async Task ProcessDatasetWithRetry(DatasetConfig dataset)
        {
            var attempt = 0;
            var success = false;

            while (attempt < dataset.MaxRetries && !success)
            {
                attempt++;
                _logger.LogInformation("Processing dataset {DatasetName}, attempt {Attempt}/{MaxRetries}",
                    dataset.Name, attempt, dataset.MaxRetries);

                success = await _powerBIService.RefreshDatasetAsync(dataset);

                if (!success && attempt < dataset.MaxRetries)
                {
                    var delay = TimeSpan.FromMinutes(dataset.RetryDelayMinutes * attempt); // Exponential backoff
                    _logger.LogWarning("Dataset {DatasetName} refresh failed, retrying in {Delay}",
                        dataset.Name, delay);
                    await Task.Delay(delay);
                }
            }

            if (!success)
            {
                _logger.LogError("Dataset {DatasetName} failed after {MaxRetries} attempts",
                    dataset.Name, dataset.MaxRetries);
            }
        }
    }
}
