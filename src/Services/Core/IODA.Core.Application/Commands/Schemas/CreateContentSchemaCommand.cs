using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Schemas;

public record CreateContentSchemaCommand(
    Guid ProjectId,
    string SchemaName,
    string SchemaType,
    string? Description,
    List<CreateSchemaFieldDto> Fields,
    Guid CreatedBy,
    Guid? ParentSchemaId = null,
    IReadOnlyList<AllowedBlockTypeRuleDto>? AllowedBlockTypes = null) : IRequest<Guid>;
