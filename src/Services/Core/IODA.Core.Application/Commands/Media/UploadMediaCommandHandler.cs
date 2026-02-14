using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Media;

public class UploadMediaCommandHandler : IRequestHandler<UploadMediaCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaStorage _storage;

    public UploadMediaCommandHandler(IUnitOfWork unitOfWork, IMediaStorage storage)
    {
        _unitOfWork = unitOfWork;
        _storage = storage;
    }

    public async Task<Guid> Handle(UploadMediaCommand request, CancellationToken cancellationToken)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            throw new ProjectNotFoundException(request.ProjectId);

        var storageKey = await _storage.SaveAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.ProjectId,
            cancellationToken);

        var displayName = request.DisplayName ?? request.FileName;
        var mediaItem = MediaItem.Create(
            request.ProjectId,
            request.FileName,
            displayName,
            request.ContentType,
            request.SizeBytes,
            storageKey,
            request.CreatedBy,
            request.Metadata);

        await _unitOfWork.MediaItems.AddAsync(mediaItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return mediaItem.Id;
    }
}
