using IODA.Core.Application.DTOs;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Tags;

public class ListTagsByProjectQueryHandler : IRequestHandler<ListTagsByProjectQuery, IReadOnlyList<TagDto>>
{
    private readonly ITagRepository _tagRepository;

    public ListTagsByProjectQueryHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<IReadOnlyList<TagDto>> Handle(ListTagsByProjectQuery request, CancellationToken cancellationToken)
    {
        var tags = await _tagRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        return tags.Select(t => new TagDto(t.Id, t.ProjectId, t.Name, t.Slug)).ToList();
    }
}
