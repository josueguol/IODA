using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using EnvironmentEntity = IODA.Core.Domain.Entities.Environment;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class EnvironmentRepository : IEnvironmentRepository
{
    private readonly CoreDbContext _context;

    public EnvironmentRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EnvironmentEntity>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Environments
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<EnvironmentEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Environments
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<EnvironmentEntity> AddAsync(EnvironmentEntity environment, CancellationToken cancellationToken = default)
    {
        await _context.Environments.AddAsync(environment, cancellationToken);
        return environment;
    }
}
