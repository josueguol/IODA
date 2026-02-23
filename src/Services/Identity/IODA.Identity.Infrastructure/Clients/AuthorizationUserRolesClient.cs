using System.Net.Http.Json;
using IODA.Identity.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace IODA.Identity.Infrastructure.Clients;

/// <summary>
/// Obtiene los nombres de roles del usuario desde Authorization API (GET users/{userId}/roles).
/// Usa el mismo HttpClient que AuthorizationEffectivePermissionsClient (AuthorizationApi).
/// </summary>
public class AuthorizationUserRolesClient : IUserRolesClient
{
    public const string HttpClientName = "AuthorizationApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthorizationUserRolesClient> _logger;

    public AuthorizationUserRolesClient(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthorizationUserRolesClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        try
        {
            var response = await client.GetAsync($"users/{userId}/roles", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Authorization API users/roles returned {StatusCode} for user {UserId}. Response: {Body}",
                    response.StatusCode, userId, body);
                return Array.Empty<string>();
            }
            var names = await response.Content.ReadFromJsonAsync<string[]>(cancellationToken);
            return names ?? Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get role names for user {UserId}", userId);
            return Array.Empty<string>();
        }
    }
}
