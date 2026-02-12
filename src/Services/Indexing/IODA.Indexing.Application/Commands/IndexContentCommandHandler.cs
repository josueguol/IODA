using IODA.Indexing.Application.Interfaces;
using MediatR;

namespace IODA.Indexing.Application.Commands;

public class IndexContentCommandHandler : IRequestHandler<IndexContentCommand>
{
    private readonly IContentIndexer _indexer;

    public IndexContentCommandHandler(IContentIndexer indexer)
    {
        _indexer = indexer;
    }

    public async Task Handle(IndexContentCommand request, CancellationToken cancellationToken)
    {
        var indexRequest = new IndexContentRequest(
            request.ContentId,
            request.VersionId,
            request.Title,
            request.ContentType,
            request.PublishedAt,
            request.Fields);
        await _indexer.IndexAsync(indexRequest, cancellationToken);
    }
}
