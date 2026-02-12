using System.Text.Json;
using IODA.Publishing.Application.Exceptions;
using IODA.Publishing.Application.Interfaces;
using IODA.Publishing.Domain.Exceptions;
using IODA.Publishing.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IODA.Publishing.Application.Commands;

public class ApprovePublicationCommandHandler : IRequestHandler<ApprovePublicationCommand>
{
    private readonly IPublicationRequestRepository _repository;
    private readonly IContentValidator _contentValidator;
    private readonly ICorePublishClient _corePublishClient;
    private readonly ILogger<ApprovePublicationCommandHandler> _logger;

    public ApprovePublicationCommandHandler(
        IPublicationRequestRepository repository,
        IContentValidator contentValidator,
        ICorePublishClient corePublishClient,
        ILogger<ApprovePublicationCommandHandler> logger)
    {
        _repository = repository;
        _contentValidator = contentValidator;
        _corePublishClient = corePublishClient;
        _logger = logger;
    }

    public async Task Handle(ApprovePublicationCommand request, CancellationToken cancellationToken)
    {
        var publicationRequest = await _repository.GetByIdAsync(request.PublicationRequestId, cancellationToken)
            ?? throw new PublicationRequestNotFoundException(request.PublicationRequestId);

        // Si la solicitud ya fue resuelta (aprobada o rechazada), no se puede volver a aprobar.
        if (publicationRequest.Status != Publishing.Domain.Entities.PublicationRequestStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot approve request {request.PublicationRequestId}: current status is {publicationRequest.Status}. Only Pending requests can be approved.");
        }

        var validation = await _contentValidator.ValidateAsync(
            publicationRequest.ProjectId,
            publicationRequest.ContentId,
            cancellationToken);

        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors);
            publicationRequest.SetValidationErrors(errors);
            publicationRequest.Reject(request.ApprovedBy, "Validation failed: " + errors);
            await _repository.UpdateAsync(publicationRequest, cancellationToken);

            _logger.LogWarning(
                "Publication request {RequestId} rejected due to validation: {Errors}",
                request.PublicationRequestId, errors);

            throw new InvalidOperationException("Content validation failed: " + errors);
        }

        try
        {
            await _corePublishClient.PublishAsync(
                publicationRequest.ProjectId,
                publicationRequest.ContentId,
                request.ApprovedBy,
                cancellationToken);
        }
        catch (CoreApiException coreEx)
        {
            // Guardar los errores de Core API en la solicitud y rechazarla
            var errorDetails = FormatCoreApiError(coreEx);
            publicationRequest.SetValidationErrors(errorDetails);
            publicationRequest.Reject(request.ApprovedBy, $"Core API error ({coreEx.StatusCode}): {coreEx.Message}");
            await _repository.UpdateAsync(publicationRequest, cancellationToken);
            
            _logger.LogError(coreEx, 
                "Core API returned {StatusCode} when publishing content {ContentId}. ProblemDetails: {ProblemDetails}",
                coreEx.StatusCode,
                publicationRequest.ContentId,
                coreEx.ProblemDetails != null ? JsonSerializer.Serialize(coreEx.ProblemDetails) : "none");
            
            throw new InvalidOperationException($"Failed to publish content: {coreEx.Message}", coreEx);
        }

        publicationRequest.Approve(request.ApprovedBy);
        await _repository.UpdateAsync(publicationRequest, cancellationToken);
    }

    private static string FormatCoreApiError(CoreApiException ex)
    {
        if (ex.ProblemDetails == null)
            return ex.Message;

        var parts = new List<string> { ex.ProblemDetails.Detail ?? ex.Message };
        
        if (ex.ProblemDetails.Extensions != null && 
            ex.ProblemDetails.Extensions.TryGetValue("errors", out var errorsObj))
        {
            if (errorsObj is JsonElement errorsElement && errorsElement.ValueKind == JsonValueKind.Object)
            {
                var errors = JsonSerializer.Serialize(errorsElement);
                parts.Add($"Validation errors: {errors}");
            }
        }

        return string.Join(" | ", parts);
    }
}
