using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerBiUpdater.Models
{
    public class PowerBIConfig
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Authority => $"https://login.microsoftonline.com/{TenantId}";
        public string[] Scopes { get; set; } = { "https://analysis.windows.net/powerbi/api/.default" };
        public List<DatasetConfig> Datasets { get; set; } = new();
    }

    public class DatasetConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string WorkspaceId { get; set; } = string.Empty;
        public int MaxRetries { get; set; } = 3;
        public int RetryDelayMinutes { get; set; } = 5;
    }

    public class ScheduleConfig
    {
        public int IntervalMinutes { get; set; } = 60;
        public List<string> RunTimes { get; set; } = new();
        public bool RunOnStartup { get; set; } = true;
    }

    public class RefreshStatus
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RefreshHistory
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class RefreshHistoryResponse
    {
        public List<RefreshHistory> Value { get; set; } = new();
    }
}
