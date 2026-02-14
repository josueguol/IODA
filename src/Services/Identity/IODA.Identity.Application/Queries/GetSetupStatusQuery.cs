using IODA.Identity.Application.DTOs;
using MediatR;

namespace IODA.Identity.Application.Queries;

public record GetSetupStatusQuery : IRequest<SetupStatusDto>;
