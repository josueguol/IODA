using IODA.Indexing.Application.Interfaces;
using IODA.Shared.Contracts.Events.V1;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace IODA.Indexing.Infrastructure.Messaging;

public class ContentPublishedEventV1Consumer : IConsumer<ContentPublishedEventV1>
{
    private readonly IContentIndexer _indexer;
    private readonly ILogger<ContentPublishedEventV1Consumer> _logger;

    public ContentPublishedEventV1Consumer(IContentIndexer indexer, ILogger<ContentPublishedEventV1Consumer> logger)
    {
        _indexer = indexer;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ContentPublishedEventV1> context)
    {
        var ev = context.Message;
        _logger.LogInformation("Consumed ContentPublishedEventV1 for ContentId {ContentId}, indexing", ev.ContentId);

        var request = new IndexContentRequest(
            ev.ContentId,
            ev.VersionId,
            ev.Title,
            ev.ContentType,
            ev.PublishedAt,
            null);
        await _indexer.IndexAsync(request, context.CancellationToken);
    }
}
