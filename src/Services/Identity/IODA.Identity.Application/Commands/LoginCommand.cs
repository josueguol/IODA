using IODA.Identity.Application.DTOs;
using MediatR;

namespace IODA.Identity.Application.Commands;

public record LoginCommand(string Email, string Password) : IRequest<LoginResultDto>;
