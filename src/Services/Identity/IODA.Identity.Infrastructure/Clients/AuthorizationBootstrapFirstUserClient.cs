using System.Net.Http.Json;
using IODA.Identity.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace IODA.Identity.Infrastructure.Clients;

public class AuthorizationBootstrapFirstUserClient : IFirstUserBootstrapClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthorizationBootstrapFirstUserClient> _logger;

    public AuthorizationBootstrapFirstUserClient(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthorizationBootstrapFirstUserClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task BootstrapFirstUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(AuthorizationEffectivePermissionsClient.HttpClientName);
        try
        {
            var response = await client.PostAsJsonAsync("bootstrap-first-user", new { UserId = userId }, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("First user {UserId} assigned SuperAdmin role in Authorization", userId);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Authorization bootstrap-first-user returned {StatusCode} for user {UserId}. Response: {Body}", response.StatusCode, userId, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to bootstrap first user {UserId} in Authorization", userId);
        }
    }
}
