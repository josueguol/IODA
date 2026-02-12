using IODA.Indexing.Application.Interfaces;
using IODA.Shared.Contracts.Events.V1;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace IODA.Indexing.Infrastructure.Messaging;

public class ContentUnpublishedEventV1Consumer : IConsumer<ContentUnpublishedEventV1>
{
    private readonly IContentIndexer _indexer;
    private readonly ILogger<ContentUnpublishedEventV1Consumer> _logger;

    public ContentUnpublishedEventV1Consumer(IContentIndexer indexer, ILogger<ContentUnpublishedEventV1Consumer> logger)
    {
        _indexer = indexer;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ContentUnpublishedEventV1> context)
    {
        var ev = context.Message;
        _logger.LogInformation("Consumed ContentUnpublishedEventV1 for ContentId {ContentId}, removing from index", ev.ContentId);
        await _indexer.RemoveAsync(ev.ContentId, context.CancellationToken);
    }
}
