using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Hierarchies;

public class DeleteHierarchyCommandHandler : IRequestHandler<DeleteHierarchyCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteHierarchyCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteHierarchyCommand request, CancellationToken cancellationToken)
    {
        var hierarchy = await _unitOfWork.Hierarchies.GetByIdAsync(request.HierarchyId, cancellationToken);
        if (hierarchy == null)
            return;

        var hasChildren = await _unitOfWork.Hierarchies.HasChildrenAsync(request.HierarchyId, cancellationToken);
        if (hasChildren)
            throw new InvalidOperationException("Cannot delete a hierarchy that has children. Delete or reassign children first.");

        await _unitOfWork.Hierarchies.DeleteAsync(hierarchy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
