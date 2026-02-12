using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Core.Infrastructure.Persistence.Repositories;

public class ContentSchemaRepository : IContentSchemaRepository
{
    private readonly CoreDbContext _context;

    public ContentSchemaRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<ContentSchema?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ContentSchemas
            .Include(s => s.Fields.OrderBy(f => f.DisplayOrder))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<ContentSchema?> GetByTypeAsync(
        Guid projectId,
        string schemaType,
        CancellationToken cancellationToken = default)
    {
        return await _context.ContentSchemas
            .Include(s => s.Fields.OrderBy(f => f.DisplayOrder))
            .FirstOrDefaultAsync(s =>
                s.ProjectId == projectId &&
                s.SchemaType == schemaType,
                cancellationToken);
    }

    public async Task<IEnumerable<ContentSchema>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ContentSchemas
            .Include(s => s.Fields.OrderBy(f => f.DisplayOrder))
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.SchemaName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ContentSchema>> GetActiveByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ContentSchemas
            .Include(s => s.Fields.OrderBy(f => f.DisplayOrder))
            .Where(s => s.ProjectId == projectId && s.IsActive)
            .OrderBy(s => s.SchemaName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentSchema> AddAsync(
        ContentSchema schema,
        CancellationToken cancellationToken = default)
    {
        await _context.ContentSchemas.AddAsync(schema, cancellationToken);
        return schema;
    }

    public Task UpdateAsync(ContentSchema schema, CancellationToken cancellationToken = default)
    {
        _context.ContentSchemas.Update(schema);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schema = await _context.ContentSchemas.FindAsync([id], cancellationToken);
        if (schema != null)
        {
            _context.ContentSchemas.Remove(schema);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ContentSchemas.AnyAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> TypeExistsAsync(
        Guid projectId,
        string schemaType,
        CancellationToken cancellationToken = default)
    {
        return await _context.ContentSchemas.AnyAsync(s =>
            s.ProjectId == projectId &&
            s.SchemaType == schemaType,
            cancellationToken);
    }
}
