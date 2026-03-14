using IODA.Core.Application.DTOs;
using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Media;

public class ReplaceMediaFileCommandHandler : IRequestHandler<ReplaceMediaFileCommand, MediaItemDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaStorage _storage;
    private readonly IMediaProcessingQueue _processingQueue;

    public ReplaceMediaFileCommandHandler(IUnitOfWork unitOfWork, IMediaStorage storage, IMediaProcessingQueue processingQueue)
    {
        _unitOfWork = unitOfWork;
        _storage = storage;
        _processingQueue = processingQueue;
    }

    public async Task<MediaItemDto> Handle(ReplaceMediaFileCommand request, CancellationToken cancellationToken)
    {
        var mediaItem = await _unitOfWork.MediaItems.GetByIdAsync(request.MediaItemId, cancellationToken);
        if (mediaItem == null || mediaItem.ProjectId != request.ProjectId)
            throw new MediaItemNotFoundException(request.MediaItemId);

        var storageKey = await _storage.SaveAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.ProjectId,
            cancellationToken);

        mediaItem.ReplaceFile(
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            storageKey,
            request.DisplayName);

        var mergedMetadata = request.Metadata ?? mediaItem.Metadata;
        mediaItem.UpdateMetadata(MediaProcessingMetadata.WithPendingStatus(mergedMetadata));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _processingQueue.Enqueue(mediaItem.Id);
        return mediaItem.ToDto();
    }
}
