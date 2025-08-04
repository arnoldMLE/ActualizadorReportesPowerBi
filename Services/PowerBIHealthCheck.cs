using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerBiUpdater.Services
{
    public class PowerBIHealthCheck : IHealthCheck
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<PowerBIHealthCheck> _logger;

        public PowerBIHealthCheck(IAuthenticationService authService, ILogger<PowerBIHealthCheck> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _authService.GetAccessTokenAsync();
                return string.IsNullOrEmpty(token)
                    ? HealthCheckResult.Unhealthy("Failed to acquire Power BI access token")
                    : HealthCheckResult.Healthy("Power BI authentication successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return HealthCheckResult.Unhealthy("Power BI authentication failed", ex);
            }
        }
    }
}
