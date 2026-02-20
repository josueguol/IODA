using System.Text.RegularExpressions;
using IODA.Core.Application.DTOs;
using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Hierarchies;

public class CreateHierarchyCommandHandler : IRequestHandler<CreateHierarchyCommand, HierarchyDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateHierarchyCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<HierarchyDto> Handle(CreateHierarchyCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Regex.Replace(request.Name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-')
            : request.Slug.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(slug))
            throw new ArgumentException("Hierarchy slug cannot be empty.", nameof(request.Name));

        if (await _unitOfWork.Hierarchies.ExistsWithSlugAsync(request.ProjectId, slug, null, cancellationToken))
            throw new InvalidOperationException($"A hierarchy with slug '{slug}' already exists in this project.");

        if (request.ParentHierarchyId.HasValue)
        {
            var parent = await _unitOfWork.Hierarchies.GetByIdAsync(request.ParentHierarchyId.Value, cancellationToken);
            if (parent == null)
                throw new InvalidOperationException("Parent hierarchy not found.");
            if (parent.ProjectId != request.ProjectId)
                throw new InvalidOperationException("Parent hierarchy must belong to the same project.");
        }

        var hierarchy = Hierarchy.Create(
            request.ProjectId,
            request.Name.Trim(),
            slug,
            request.Description,
            request.ImageUrl,
            request.ParentHierarchyId);

        await _unitOfWork.Hierarchies.AddAsync(hierarchy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new HierarchyDto(
            hierarchy.Id,
            hierarchy.ProjectId,
            hierarchy.Name,
            hierarchy.Slug,
            hierarchy.Description,
            hierarchy.ImageUrl,
            hierarchy.ParentHierarchyId);
    }
}
