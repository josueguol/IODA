using IODA.Publishing.Domain.Repositories;
using MediatR;

namespace IODA.Publishing.Application.Queries;

public class GetPublicationRequestsQueryHandler : IRequestHandler<GetPublicationRequestsQuery, IReadOnlyList<PublicationRequestDto>>
{
    private readonly IPublicationRequestRepository _repository;

    public GetPublicationRequestsQueryHandler(IPublicationRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PublicationRequestDto>> Handle(GetPublicationRequestsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.PublicationRequest> list;
        if (request.ContentId.HasValue)
            list = await _repository.GetByContentIdAsync(request.ContentId.Value, cancellationToken);
        else if (request.Status.HasValue)
            list = await _repository.GetByStatusAsync(request.Status.Value, cancellationToken);
        else
            list = await _repository.GetAllAsync(cancellationToken);

        return list.Select(r => new PublicationRequestDto(
            r.Id,
            r.ContentId,
            r.ProjectId,
            r.EnvironmentId,
            r.RequestedBy,
            r.Status,
            r.RequestedAt,
            r.ResolvedAt,
            r.ResolvedBy,
            r.RejectionReason,
            r.ValidationErrors
        )).ToList();
    }
}
