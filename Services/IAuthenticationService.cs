using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using PowerBiUpdater.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PowerBiUpdater.Services
{
    public interface IAuthenticationService
    {
        Task<string> GetAccessTokenAsync();
    }
    public class AuthenticationService : IAuthenticationService
    {
        private readonly PowerBIConfig _config;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IConfidentialClientApplication _app;
        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public AuthenticationService(IOptions<PowerBIConfig> config, ILogger<AuthenticationService> logger)
        {
            _config = config.Value;
            _logger = logger;

            _app = ConfidentialClientApplicationBuilder
                .Create(_config.ClientId)
                .WithClientSecret(_config.ClientSecret)
                .WithAuthority(_config.Authority)
                .Build();
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // Return cached token if still valid (with 5-minute buffer)
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
            {
                return _cachedToken;
            }

            try
            {
                var result = await _app.AcquireTokenForClient(_config.Scopes).ExecuteAsync();
                _cachedToken = result.AccessToken;
                _tokenExpiry = result.ExpiresOn.UtcDateTime;

                _logger.LogInformation("Successfully acquired new access token, expires at {ExpiryTime}", _tokenExpiry);
                return _cachedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire access token");
                throw;
            }
        }
    }
}
