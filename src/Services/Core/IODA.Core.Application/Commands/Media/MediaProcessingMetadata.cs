namespace IODA.Core.Application.Commands.Media;

internal static class MediaProcessingMetadata
{
    public static Dictionary<string, object> WithPendingStatus(Dictionary<string, object>? metadata)
    {
        var next = metadata != null
            ? new Dictionary<string, object>(metadata)
            : new Dictionary<string, object>();

        next["processingStatus"] = "pending";
        next["processingRequestedAt"] = DateTime.UtcNow;
        next.Remove("processingError");
        return next;
    }
}
