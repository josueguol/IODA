using IODA.Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IODA.Core.Infrastructure.Storage;

public class MediaPublicUrlBuilder : IMediaPublicUrlBuilder
{
    private readonly string? _publicBaseUrl;

    public MediaPublicUrlBuilder(IConfiguration configuration)
    {
        _publicBaseUrl = configuration["Core:PublicBaseUrl"]?.TrimEnd('/');
    }

    public string BuildFileUrl(Guid projectId, Guid mediaId, string? variant = null)
    {
        var path = $"/api/projects/{projectId}/media/{mediaId}/file";
        if (!string.IsNullOrWhiteSpace(variant))
            path += $"?variant={Uri.EscapeDataString(variant)}";

        return string.IsNullOrWhiteSpace(_publicBaseUrl)
            ? path
            : $"{_publicBaseUrl}{path}";
    }
}
