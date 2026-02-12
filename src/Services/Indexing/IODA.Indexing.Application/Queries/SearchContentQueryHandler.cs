using IODA.Indexing.Application.Interfaces;
using MediatR;

namespace IODA.Indexing.Application.Queries;

public class SearchContentQueryHandler : IRequestHandler<SearchContentQuery, SearchResult>
{
    private readonly IContentIndexer _indexer;

    public SearchContentQueryHandler(IContentIndexer indexer)
    {
        _indexer = indexer;
    }

    public async Task<SearchResult> Handle(SearchContentQuery request, CancellationToken cancellationToken)
    {
        var query = new SearchQuery(request.Query, request.Page, request.PageSize, request.ContentType);
        return await _indexer.SearchAsync(query, cancellationToken);
    }
}
