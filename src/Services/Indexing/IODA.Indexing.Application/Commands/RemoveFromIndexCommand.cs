using MediatR;

namespace IODA.Indexing.Application.Commands;

public record RemoveFromIndexCommand(Guid ContentId) : IRequest;
