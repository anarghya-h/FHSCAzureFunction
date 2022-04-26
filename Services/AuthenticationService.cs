using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FHSCAzureFunction.Models.Configs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace FHSCAzureFunction.Services
{
    public class AuthenticationService : IHostedService, IDisposable
    {
        #region Private members variables
        private readonly SDxConfig sdxConfig;
        private readonly ILogger<AuthenticationService> logger;
        private Timer refreshTokenTimer;
        #endregion

        #region Public members
        public TokenResponse tokenResponse;
        #endregion

        #region Constructors
        public AuthenticationService(SDxConfig sdxConfig, ILogger<AuthenticationService> logger)
        {
            this.logger = logger;
            this.sdxConfig = sdxConfig;
        }
        #endregion

        #region Public Methods
        public Task StartAsync(CancellationToken stoppingToken)
        {
            tokenResponse = GetOAuthTokenClientCredentialsFlow(sdxConfig.AuthServerAuthority, sdxConfig.AuthClientId, sdxConfig.AuthClientSecret, sdxConfig.ServerResourceID);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Stoping token refresh thread...");

            this.refreshTokenTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            logger.LogInformation("AuthenticationService disposing...");

            this.refreshTokenTimer?.Dispose();
        }
        #endregion

        #region Private Methods
        private TokenResponse GetOAuthTokenClientCredentialsFlow(string authServerAuthority, string authClientId, string authClientSecret, string serverResourceID)
        {
            var client = new HttpClient();

            var discoveryDocument = client.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = authServerAuthority,
                Policy =
                    {
                        ValidateEndpoints = false,
                        ValidateIssuerName = false,
                        RequireHttps = true
                    }
            }).Result;

            //var parameters = string.IsNullOrEmpty(serverResourceID) ? new Dictionary<string, string>() : new Dictionary<string, string>() { { "Resource", serverResourceID } };
            var parameters = new Parameters(string.IsNullOrEmpty(serverResourceID) ? new Dictionary<string, string>() : new Dictionary<string, string>() { { "Resource", serverResourceID } });

            var tokenResponse = client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = authClientId,
                ClientSecret = authClientSecret,
                Scope = serverResourceID,
                Parameters = parameters
            }).Result;

            if (!string.IsNullOrWhiteSpace(tokenResponse.Error))
            {
                logger.LogError(tokenResponse.Error);
                throw new Exception(tokenResponse.Error);
            }

            if (!string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                logger.LogInformation("Token obtained successfully");
            }

            TokenExpirationCall(tokenResponse.ExpiresIn, authServerAuthority, authClientId, authClientSecret, serverResourceID);

            return tokenResponse;
        }

        private void TokenExpirationCall(int timeTillExpires, string authServerAuthority, string authClientId, string authClientSecret, string serverResourceID)
        {
            // Sets token refresh callback before the token expires so that we don't get 401's by using expired tokens
            this.refreshTokenTimer = new Timer(_ =>
            {
                logger.LogInformation("Token expiring obtaining new one...");
                this.tokenResponse = GetOAuthTokenClientCredentialsFlow(authServerAuthority, authClientId, authClientSecret, serverResourceID);
            }
                , null, (int)TimeSpan.FromSeconds(timeTillExpires - 30).TotalMilliseconds, Timeout.Infinite);
            logger.LogInformation("Refresh token timer initialized successfully");
        }

        #endregion
    }
}
