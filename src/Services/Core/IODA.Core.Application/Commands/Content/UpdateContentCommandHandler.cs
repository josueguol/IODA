using IODA.Core.Application.DTOs;
using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using IODA.Shared.Contracts.Events.V1;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public class UpdateContentCommandHandler : IRequestHandler<UpdateContentCommand, ContentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ISchemaValidationService _schemaValidation;

    public UpdateContentCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher, ISchemaValidationService schemaValidation)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _schemaValidation = schemaValidation;
    }

    public async Task<ContentDto> Handle(UpdateContentCommand request, CancellationToken cancellationToken)
    {
        var content = await _unitOfWork.Contents.GetByIdAsync(request.ContentId, cancellationToken);
        if (content == null)
        {
            throw new ContentNotFoundException(request.ContentId);
        }

        var schema = await _unitOfWork.Schemas.GetByIdAsync(content.SchemaId, cancellationToken);
        if (schema == null)
        {
            throw new InvalidOperationException($"Schema with ID '{content.SchemaId}' was not found.");
        }

        var validationResult = _schemaValidation.Validate(schema, request.Fields ?? new Dictionary<string, object>());
        if (!validationResult.IsValid)
        {
            throw new SchemaValidationException(validationResult.Errors
                .Select(e => new SchemaValidationErrorEntry(e.Field, e.Message))
                .ToList());
        }

        content.Update(request.Title, request.Fields, request.UpdatedBy);

        await _unitOfWork.Contents.UpdateAsync(content, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var latestVersion = content.GetLatestVersion();
        var integrationEvent = new ContentUpdatedEventV1
        {
            ContentId = content.Id,
            VersionId = latestVersion.Id,
            VersionNumber = content.CurrentVersion,
            Title = content.Title,
            Status = content.Status.Value,
            UpdatedBy = request.UpdatedBy,
            Fields = content.Fields
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        return content.ToDto();
    }
}
