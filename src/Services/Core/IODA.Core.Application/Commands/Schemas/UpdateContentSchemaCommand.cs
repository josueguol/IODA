using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Schemas;

public record UpdateContentSchemaCommand(
    Guid ProjectId,
    Guid SchemaId,
    string SchemaName,
    string SchemaType,
    string? Description,
    List<UpdateSchemaFieldDto> Fields,
    Guid UpdatedBy,
    IReadOnlyList<AllowedBlockTypeRuleDto>? AllowedBlockTypes = null) : IRequest;

