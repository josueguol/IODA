using IODA.Publishing.Domain.Entities;
using IODA.Publishing.Domain.Repositories;
using MediatR;

namespace IODA.Publishing.Application.Commands;

public class RequestPublicationCommandHandler : IRequestHandler<RequestPublicationCommand, Guid>
{
    private readonly IPublicationRequestRepository _repository;

    public RequestPublicationCommandHandler(IPublicationRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(RequestPublicationCommand request, CancellationToken cancellationToken)
    {
        var publicationRequest = PublicationRequest.Create(
            request.ContentId,
            request.ProjectId,
            request.EnvironmentId,
            request.RequestedBy);
        await _repository.AddAsync(publicationRequest, cancellationToken);
        return publicationRequest.Id;
    }
}
