using IODA.Publishing.Application.Interfaces;
using IODA.Publishing.Infrastructure.CoreApi;
using Microsoft.Extensions.Logging;

namespace IODA.Publishing.Infrastructure.Validation;

public class ContentValidator : IContentValidator
{
    private readonly CorePublishClient _coreClient;
    private readonly ILogger<ContentValidator> _logger;

    public ContentValidator(CorePublishClient coreClient, ILogger<ContentValidator> logger)
    {
        _coreClient = coreClient;
        _logger = logger;
    }

    public async Task<ContentValidationResult> ValidateAsync(Guid projectId, Guid contentId, CancellationToken cancellationToken = default)
    {
        var content = await _coreClient.GetContentAsync(projectId, contentId, cancellationToken);
        if (content == null)
        {
            _logger.LogWarning("Content {ContentId} not found in Core API", contentId);
            return new ContentValidationResult(false, ["Content not found in Core API."]);
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(content.Title))
            errors.Add("Title is required and cannot be empty.");

        if (content.Status.Equals("Published", StringComparison.OrdinalIgnoreCase))
            errors.Add("Content is already published.");

        if (content.Fields == null || content.Fields.Count == 0)
            errors.Add("Content must have at least one field.");

        if (errors.Count > 0)
            return new ContentValidationResult(false, errors);

        return new ContentValidationResult(true, []);
    }
}
