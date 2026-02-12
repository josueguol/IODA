using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using IODA.Shared.Contracts.Events;
using IODA.Shared.Contracts.Events.V1;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public class CreateContentCommandHandler : IRequestHandler<CreateContentCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ISchemaValidationService _schemaValidation;

    public CreateContentCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher, ISchemaValidationService schemaValidation)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _schemaValidation = schemaValidation;
    }

    public async Task<Guid> Handle(CreateContentCommand request, CancellationToken cancellationToken)
    {
        var schema = await _unitOfWork.Schemas.GetByIdAsync(request.SchemaId, cancellationToken);
        if (schema == null)
        {
            throw new InvalidOperationException($"Schema with ID '{request.SchemaId}' was not found.");
        }

        var validationResult = _schemaValidation.Validate(schema, request.Fields ?? new Dictionary<string, object>());
        if (!validationResult.IsValid)
        {
            throw new SchemaValidationException(validationResult.Errors
                .Select(e => new SchemaValidationErrorEntry(e.Field, e.Message))
                .ToList());
        }

        if (request.SiteId.HasValue)
        {
            var site = await _unitOfWork.Sites.GetByIdAsync(request.SiteId.Value, cancellationToken);
            if (site == null || site.ProjectId != request.ProjectId)
            {
                throw new InvalidOperationException($"Site '{request.SiteId.Value}' not found or does not belong to the project.");
            }
        }

        var fields = request.Fields ?? new Dictionary<string, object>();
        var content = Domain.Entities.Content.Create(
            request.ProjectId,
            request.EnvironmentId,
            request.SiteId,
            request.SchemaId,
            request.Title,
            request.ContentType,
            fields,
            request.CreatedBy);

        await _unitOfWork.Contents.AddAsync(content, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var integrationEvent = new ContentCreatedEventV1
        {
            ContentId = content.Id,
            Title = content.Title,
            ContentType = content.ContentType,
            Status = content.Status.Value,
            ProjectId = content.ProjectId,
            EnvironmentId = content.EnvironmentId,
            CreatedBy = request.CreatedBy,
            Fields = content.Fields
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        return content.Id;
    }
}
