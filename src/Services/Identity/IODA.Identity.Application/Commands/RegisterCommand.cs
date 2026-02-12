using MediatR;

namespace IODA.Identity.Application.Commands;

public record RegisterCommand(
    string Email,
    string Password,
    string? DisplayName = null) : IRequest<Guid>;
