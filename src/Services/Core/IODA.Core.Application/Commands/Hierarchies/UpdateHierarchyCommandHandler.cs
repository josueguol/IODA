using System.Text.RegularExpressions;
using IODA.Core.Application.DTOs;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Hierarchies;

public class UpdateHierarchyCommandHandler : IRequestHandler<UpdateHierarchyCommand, HierarchyDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateHierarchyCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<HierarchyDto> Handle(UpdateHierarchyCommand request, CancellationToken cancellationToken)
    {
        var hierarchy = await _unitOfWork.Hierarchies.GetByIdAsync(request.HierarchyId, cancellationToken);
        if (hierarchy == null)
            throw new InvalidOperationException("Hierarchy not found.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Regex.Replace(request.Name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-')
            : request.Slug.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(slug))
            throw new ArgumentException("Hierarchy slug cannot be empty.", nameof(request.Name));

        if (await _unitOfWork.Hierarchies.ExistsWithSlugAsync(hierarchy.ProjectId, slug, request.HierarchyId, cancellationToken))
            throw new InvalidOperationException($"A hierarchy with slug '{slug}' already exists in this project.");

        if (request.ParentHierarchyId.HasValue)
        {
            if (request.ParentHierarchyId.Value == hierarchy.Id)
                throw new InvalidOperationException("A hierarchy cannot be its own parent.");
            var parent = await _unitOfWork.Hierarchies.GetByIdAsync(request.ParentHierarchyId.Value, cancellationToken);
            if (parent == null)
                throw new InvalidOperationException("Parent hierarchy not found.");
            if (parent.ProjectId != hierarchy.ProjectId)
                throw new InvalidOperationException("Parent hierarchy must belong to the same project.");
            var ancestorIds = await _unitOfWork.Hierarchies.GetAncestorIdsAsync(request.ParentHierarchyId.Value, 50, cancellationToken);
            if (ancestorIds.Contains(hierarchy.Id))
                throw new InvalidOperationException("Setting this parent would create a circular reference.");
        }

        hierarchy.Update(request.Name.Trim(), slug, request.Description, request.ImageUrl);
        hierarchy.SetParent(request.ParentHierarchyId);

        await _unitOfWork.Hierarchies.UpdateAsync(hierarchy, cancellationToken);
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
