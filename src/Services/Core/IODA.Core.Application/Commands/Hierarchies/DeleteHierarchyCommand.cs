using MediatR;

namespace IODA.Core.Application.Commands.Hierarchies;

public record DeleteHierarchyCommand(Guid HierarchyId) : IRequest;
