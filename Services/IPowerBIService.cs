using Microsoft.Extensions.Options;
using PowerBiUpdater.Models;
using PowerBiUpdater.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PowerBiUpdater.Services
{
    public interface IPowerBIService
    {
        Task<bool> RefreshDatasetAsync(DatasetConfig dataset);
        Task<RefreshStatus> GetRefreshStatusAsync(string workspaceId, string datasetId, string refreshId);
        Task<List<RefreshHistory>> GetRefreshHistoryAsync(string workspaceId, string datasetId);
    }

    public class PowerBIService : IPowerBIService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<PowerBIService> _logger;
        private const string BaseUrl = "https://api.powerbi.com/v1.0/myorg";

        public PowerBIService(HttpClient httpClient, IAuthenticationService authService, ILogger<PowerBIService> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
        }

        public async Task<bool> RefreshDatasetAsync(DatasetConfig dataset)
        {
            try
            {
                var token = await _authService.GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = $"{BaseUrl}/groups/{dataset.WorkspaceId}/datasets/{dataset.Id}/refreshes";

                var refreshRequest = new
                {
                    type = "full",
                    commitMode = "transactional",
                    maxParallelism = 2,
                    retryCount = dataset.MaxRetries,
                    objects = new object[0] // Empty array means refresh all tables
                };

                var json = JsonSerializer.Serialize(refreshRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully initiated refresh for dataset {DatasetName} ({DatasetId})",
                        dataset.Name, dataset.Id);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to refresh dataset {DatasetName}. Status: {StatusCode}, Error: {Error}",
                        dataset.Name, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while refreshing dataset {DatasetName}", dataset.Name);
                return false;
            }
        }

        public async Task<RefreshStatus> GetRefreshStatusAsync(string workspaceId, string datasetId, string refreshId)
        {
            try
            {
                var token = await _authService.GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = $"{BaseUrl}/groups/{workspaceId}/datasets/{datasetId}/refreshes/{refreshId}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<RefreshStatus>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new RefreshStatus();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get refresh status for dataset {DatasetId}", datasetId);
            }

            return new RefreshStatus { Status = "Unknown" };
        }

        public async Task<List<RefreshHistory>> GetRefreshHistoryAsync(string workspaceId, string datasetId)
        {
            try
            {
                var token = await _authService.GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = $"{BaseUrl}/groups/{workspaceId}/datasets/{datasetId}/refreshes?$top=10";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RefreshHistoryResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result?.Value ?? new List<RefreshHistory>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get refresh history for dataset {DatasetId}", datasetId);
            }

            return new List<RefreshHistory>();
        }
    }
}
