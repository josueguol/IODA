using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Schemas;
using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using IODA.Shared.Contracts.Events.V1;
using MediatR;

namespace IODA.Core.Application.Commands.Schemas;

public class UpdateContentSchemaCommandHandler : IRequestHandler<UpdateContentSchemaCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public UpdateContentSchemaCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UpdateContentSchemaCommand request, CancellationToken cancellationToken)
    {
        var schema = await _unitOfWork.Schemas.GetByIdAsync(request.SchemaId, cancellationToken)
            ?? throw new SchemaNotFoundException(request.SchemaId);

        if (schema.ProjectId != request.ProjectId)
            throw new SchemaNotFoundException(request.SchemaId);

        if (!string.Equals(schema.SchemaType, request.SchemaType, StringComparison.Ordinal))
            throw new InvalidOperationException("SchemaType is immutable once created.");

        var existingFields = schema.Fields.ToList();
        var existingById = existingFields.ToDictionary(f => f.Id, f => f);

        foreach (var incoming in request.Fields)
        {
            // Field identity is the persistent Id. Deleted fields are represented by omission from request.Fields,
            // so a re-added field with same slug but null Id must be treated as a brand-new field.
            if (!incoming.Id.HasValue) continue;
            if (!existingById.TryGetValue(incoming.Id.Value, out var existing))
                throw new InvalidOperationException($"Field id '{incoming.Id}' does not belong to the target schema.");

            var existingType = FieldTypeCanonicalizer.Canonicalize(existing.FieldType);
            var incomingType = FieldTypeCanonicalizer.Canonicalize(incoming.FieldType);

            if (!string.Equals(existingType, incomingType, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"FieldType for '{existing.Slug}' is immutable once the field exists.");
        }

        var nextFieldDefinitions = request.Fields
            .OrderBy(f => f.DisplayOrder)
            .Select((f, index) =>
            {
                // Preserve identity only for currently existing active fields matched by Id.
                Guid? stableId = f.Id.HasValue && existingById.ContainsKey(f.Id.Value) ? f.Id.Value : null;

                return FieldDefinition.Create(
                    Guid.Empty,
                    f.Label,
                    f.Slug,
                    FieldTypeCanonicalizer.Canonicalize(f.FieldType),
                    f.IsRequired,
                    f.DefaultValue,
                    f.HelpText,
                    f.ValidationRules,
                    f.DisplayOrder != 0 ? f.DisplayOrder : index,
                    stableId);
            })
            .ToList();

        var allowedRules = request.AllowedBlockTypes?
            .Select(r => new AllowedBlockTypeRule(r.BlockType, r.MinOccurrences, r.MaxOccurrences))
            .ToList();

        schema.UpdateDefinition(
            request.SchemaName,
            request.Description,
            nextFieldDefinitions,
            allowedRules);

        await _unitOfWork.Schemas.UpdateAsync(schema, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedEvent = new SchemaUpdatedEventV1
        {
            SchemaId = schema.Id,
            SchemaName = schema.SchemaName,
            SchemaVersion = schema.SchemaVersion,
            UpdatedBy = request.UpdatedBy,
            AddedFields = schema.Fields
                .Where(f => !existingById.ContainsKey(f.Id))
                .Select(f => new IODA.Shared.Contracts.Events.V1.FieldDefinitionDto(
                    f.Slug,
                    f.FieldType,
                    f.IsRequired,
                    f.DefaultValue,
                    f.ValidationRules))
                .ToList(),
            RemovedFields = existingFields
                .Where(old => schema.Fields.All(next => next.Id != old.Id))
                .Select(old => old.Slug)
                .ToList(),
            ModifiedFields = schema.Fields
                .Where(f => existingById.ContainsKey(f.Id))
                .Select(f => new IODA.Shared.Contracts.Events.V1.FieldDefinitionDto(
                    f.Slug,
                    f.FieldType,
                    f.IsRequired,
                    f.DefaultValue,
                    f.ValidationRules))
                .ToList()
        };

        await _eventPublisher.PublishAsync(updatedEvent, cancellationToken);
    }
}
