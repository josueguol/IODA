using System.Text.RegularExpressions;
using IODA.Core.Application.DTOs;
using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Tags;

public class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, TagDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateTagCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TagDto> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Regex.Replace(request.Name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-')
            : request.Slug.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(slug))
            throw new ArgumentException("Tag slug cannot be empty.", nameof(request.Name));

        if (await _unitOfWork.Tags.ExistsWithSlugAsync(request.ProjectId, slug, cancellationToken))
            throw new InvalidOperationException($"A tag with slug '{slug}' already exists in this project.");

        var tag = Tag.Create(request.ProjectId, request.Name.Trim(), slug);
        await _unitOfWork.Tags.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TagDto(tag.Id, tag.ProjectId, tag.Name, tag.Slug);
    }
}
