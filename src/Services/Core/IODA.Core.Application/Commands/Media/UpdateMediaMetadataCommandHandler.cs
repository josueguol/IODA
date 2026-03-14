using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Media;

public class UpdateMediaMetadataCommandHandler : IRequestHandler<UpdateMediaMetadataCommand, MediaItemDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMediaMetadataCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MediaItemDto> Handle(UpdateMediaMetadataCommand request, CancellationToken cancellationToken)
    {
        var mediaItem = await _unitOfWork.MediaItems.GetByIdAsync(request.MediaItemId, cancellationToken);
        if (mediaItem == null || mediaItem.ProjectId != request.ProjectId)
            throw new MediaItemNotFoundException(request.MediaItemId);

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            mediaItem.UpdateDisplayName(request.DisplayName.Trim());

        mediaItem.UpdateMetadata(request.Metadata);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return mediaItem.ToDto();
    }
}
