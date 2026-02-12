using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record CreateContentCommand(
    Guid ProjectId,
    Guid EnvironmentId,
    Guid? SiteId,
    Guid SchemaId,
    string Title,
    string ContentType,
    Dictionary<string, object> Fields,
    Guid CreatedBy) : IRequest<Guid>;
