using System.Net.Http.Json;
using IODA.Identity.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IODA.Identity.Infrastructure.Clients;

public class AuthorizationEffectivePermissionsClient : IEffectivePermissionsClient
{
    public const string HttpClientName = "AuthorizationApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthorizationEffectivePermissionsClient> _logger;

    public AuthorizationEffectivePermissionsClient(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthorizationEffectivePermissionsClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        try
        {
            var response = await client.GetAsync($"users/{userId}/effective-permissions", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Authorization API effective-permissions returned {StatusCode} for user {UserId}. Response: {Body}", response.StatusCode, userId, body);
                return Array.Empty<string>();
            }
            var codes = await response.Content.ReadFromJsonAsync<string[]>(cancellationToken);
            return codes ?? Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get effective permissions for user {UserId}", userId);
            return Array.Empty<string>();
        }
    }
}
