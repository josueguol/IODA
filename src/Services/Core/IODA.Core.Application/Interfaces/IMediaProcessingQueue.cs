namespace IODA.Core.Application.Interfaces;

public interface IMediaProcessingQueue
{
    void Enqueue(Guid mediaItemId);
}
