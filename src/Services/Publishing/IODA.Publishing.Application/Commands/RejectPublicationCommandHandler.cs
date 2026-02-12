using IODA.Publishing.Domain.Exceptions;
using IODA.Publishing.Domain.Repositories;
using MediatR;

namespace IODA.Publishing.Application.Commands;

public class RejectPublicationCommandHandler : IRequestHandler<RejectPublicationCommand>
{
    private readonly IPublicationRequestRepository _repository;

    public RejectPublicationCommandHandler(IPublicationRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RejectPublicationCommand request, CancellationToken cancellationToken)
    {
        var publicationRequest = await _repository.GetByIdAsync(request.PublicationRequestId, cancellationToken)
            ?? throw new PublicationRequestNotFoundException(request.PublicationRequestId);
        publicationRequest.Reject(request.RejectedBy, request.Reason);
        await _repository.UpdateAsync(publicationRequest, cancellationToken);
    }
}
