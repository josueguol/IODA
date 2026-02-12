using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Schemas;

public class ListSchemasByProjectQueryHandler : IRequestHandler<ListSchemasByProjectQuery, IReadOnlyList<ContentSchemaListItemDto>>
{
    private readonly IContentSchemaRepository _schemaRepository;

    public ListSchemasByProjectQueryHandler(IContentSchemaRepository schemaRepository)
    {
        _schemaRepository = schemaRepository;
    }

    public async Task<IReadOnlyList<ContentSchemaListItemDto>> Handle(
        ListSchemasByProjectQuery request,
        CancellationToken cancellationToken)
    {
        var schemas = request.ActiveOnly
            ? await _schemaRepository.GetActiveByProjectAsync(request.ProjectId, cancellationToken)
            : await _schemaRepository.GetByProjectAsync(request.ProjectId, cancellationToken);

        return schemas.Select(s => s.ToListItemDto()).ToList();
    }
}
