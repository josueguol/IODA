using System.Net.Http.Json;
using System.Text.Json;
using IODA.Publishing.Application.Exceptions;
using IODA.Publishing.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IODA.Publishing.Infrastructure.CoreApi;

public class CorePublishClient : ICorePublishClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CorePublishClient> _logger;

    public CorePublishClient(
        HttpClient httpClient,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CorePublishClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        var baseUrl = configuration["CoreApi:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5269";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Propaga el token JWT del request entrante al HttpClient para llamar a Core API.
    /// </summary>
    private void PropagateJwt()
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);
        }
    }

    public async Task PublishAsync(Guid projectId, Guid contentId, Guid publishedBy, CancellationToken cancellationToken = default)
    {
        PropagateJwt();
        var url = $"/api/projects/{projectId}/content/{contentId}/publish";
        var body = new { publishedBy };
        var response = await _httpClient.PostAsJsonAsync(url, body, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, projectId, contentId, cancellationToken);
        }
        
        _logger.LogInformation("Published content {ContentId} in project {ProjectId} via Core API", contentId, projectId);
    }

    private async Task HandleErrorResponseAsync(HttpResponseMessage response, Guid projectId, Guid contentId, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        ProblemDetails? problemDetails = null;
        string errorMessage = $"Core API returned {statusCode} when publishing content {contentId} in project {projectId}.";

        // Intentar leer ProblemDetails si el Content-Type es application/problem+json
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType == "application/problem+json" || contentType == "application/json")
        {
            try
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(jsonOptions, cancellationToken);
                
                if (problemDetails != null)
                {
                    errorMessage = $"Core API error ({statusCode}): {problemDetails.Title} - {problemDetails.Detail}";
                    
                    // Si hay errores de validación en Extensions, incluirlos en el mensaje
                    if (problemDetails.Extensions != null && 
                        problemDetails.Extensions.TryGetValue("errors", out var errorsObj) &&
                        errorsObj is JsonElement errorsElement)
                    {
                        var errorsText = JsonSerializer.Serialize(errorsElement);
                        errorMessage += $" Errors: {errorsText}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ProblemDetails from Core API response");
            }
        }

        _logger.LogError("Core API publish failed: {Message}", errorMessage);
        throw new CoreApiException(statusCode, errorMessage, problemDetails);
    }

    public async Task<CoreContentResponse?> GetContentAsync(Guid projectId, Guid contentId, CancellationToken cancellationToken = default)
    {
        PropagateJwt();
        var url = $"/api/projects/{projectId}/content/{contentId}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Core API GET {Url} returned {StatusCode}: {Body}",
                url, statusCode, body.Length > 500 ? body[..500] : body);
            return null;
        }
        var dto = await response.Content.ReadFromJsonAsync<CoreContentResponse>(cancellationToken);
        return dto;
    }
}
