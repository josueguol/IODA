using IODA.Identity.Application.DTOs;
using MediatR;

namespace IODA.Identity.Application.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResultDto>;
