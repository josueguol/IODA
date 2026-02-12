using IODA.Indexing.Application.Interfaces;
using MediatR;

namespace IODA.Indexing.Application.Commands;

public class RemoveFromIndexCommandHandler : IRequestHandler<RemoveFromIndexCommand>
{
    private readonly IContentIndexer _indexer;

    public RemoveFromIndexCommandHandler(IContentIndexer indexer)
    {
        _indexer = indexer;
    }

    public async Task Handle(RemoveFromIndexCommand request, CancellationToken cancellationToken)
    {
        await _indexer.RemoveAsync(request.ContentId, cancellationToken);
    }
}
