using IODA.Core.Application.Interfaces;
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

        var existingById = schema.Fields.ToDictionary(f => f.Id, f => f);
        var existingBySlug = schema.Fields.ToDictionary(f => f.Slug, f => f, StringComparer.OrdinalIgnoreCase);
        var existingSlugs = schema.Fields.Select(f => f.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var incomingSlugs = request.Fields.Select(f => f.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var incoming in request.Fields)
        {
            FieldDefinition? existing = null;
            if (incoming.Id.HasValue && existingById.TryGetValue(incoming.Id.Value, out var byId))
            {
                existing = byId;
            }
            else if (existingBySlug.TryGetValue(incoming.Slug, out var bySlug))
            {
                existing = bySlug;
            }
            else if (incoming.Id.HasValue)
            {
                // Legacy tolerance: if frontend sends a stale field Id, fall back to slug matching.
                // If neither Id nor slug exists in this schema, reject to avoid cross-schema Id injection.
                throw new InvalidOperationException($"Field id '{incoming.Id}' does not belong to the target schema.");
            }

            if (existing != null && !string.Equals(existing.FieldType, incoming.FieldType, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"FieldType for '{existing.Slug}' is immutable once the field exists.");
        }

        var nextFieldDefinitions = request.Fields
            .OrderBy(f => f.DisplayOrder)
            .Select((f, index) =>
            {
                Guid? stableId = null;
                if (f.Id.HasValue && existingById.ContainsKey(f.Id.Value))
                {
                    stableId = f.Id.Value;
                }
                else if (existingBySlug.TryGetValue(f.Slug, out var bySlug))
                {
                    stableId = bySlug.Id;
                }

                return FieldDefinition.Create(
                    Guid.Empty,
                    f.Label,
                    f.Slug,
                    f.FieldType,
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
                .Where(f => !existingSlugs.Contains(f.Slug))
                .Select(f => new IODA.Shared.Contracts.Events.V1.FieldDefinitionDto(
                    f.Slug,
                    f.FieldType,
                    f.IsRequired,
                    f.DefaultValue,
                    f.ValidationRules))
                .ToList(),
            RemovedFields = existingSlugs
                .Where(slug => !incomingSlugs.Contains(slug))
                .ToList(),
            ModifiedFields = schema.Fields
                .Where(f => existingSlugs.Contains(f.Slug))
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
