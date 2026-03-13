using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Schemas;
using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using IODA.Shared.Contracts.Events.V1;
using MediatR;

namespace IODA.Core.Application.Commands.Schemas;

public class CreateContentSchemaCommandHandler : IRequestHandler<CreateContentSchemaCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public CreateContentSchemaCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(CreateContentSchemaCommand request, CancellationToken cancellationToken)
    {
        var exists = await _unitOfWork.Schemas.TypeExistsAsync(request.ProjectId, request.SchemaType, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Schema type '{request.SchemaType}' already exists for this project.");
        }

        var fieldDefinitions = request.Fields
            .Select((f, index) => FieldDefinition.Create(
                Guid.Empty,
                f.Label,
                f.Slug,
                FieldTypeCanonicalizer.Canonicalize(f.FieldType),
                f.IsRequired,
                f.DefaultValue,
                f.HelpText,
                f.ValidationRules,
                f.DisplayOrder != 0 ? f.DisplayOrder : index))
            .ToList();

        var allowedRules = request.AllowedBlockTypes?
            .Select(r => new IODA.Core.Domain.Entities.AllowedBlockTypeRule(r.BlockType, r.MinOccurrences, r.MaxOccurrences))
            .ToList();

        var schema = ContentSchema.Create(
            request.ProjectId,
            request.SchemaName,
            request.SchemaType,
            request.Description,
            fieldDefinitions,
            request.CreatedBy,
            null,
            allowedRules);

        await _unitOfWork.Schemas.AddAsync(schema, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var integrationEvent = new SchemaCreatedEventV1
        {
            SchemaId = schema.Id,
            SchemaName = schema.SchemaName,
            SchemaType = schema.SchemaType,
            ProjectId = schema.ProjectId,
            CreatedBy = request.CreatedBy,
            Fields = schema.Fields.Select(f => new IODA.Shared.Contracts.Events.V1.FieldDefinitionDto(
                f.Slug,
                f.FieldType,
                f.IsRequired,
                f.DefaultValue,
                f.ValidationRules)).ToList()
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        return schema.Id;
    }
}
