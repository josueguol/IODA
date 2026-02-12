using IODA.Indexing.Application.Interfaces;
using MediatR;

namespace IODA.Indexing.Application.Queries;

public record SearchContentQuery(string Query, int Page = 1, int PageSize = 20, string? ContentType = null)
    : IRequest<SearchResult>;
