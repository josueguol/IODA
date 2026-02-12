using IODA.Identity.Domain.Entities;
using IODA.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IODA.Identity.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Users.FindAsync([id], cancellationToken);

    public async Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Users.OrderBy(u => u.Email).ToListAsync(cancellationToken);

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        await _context.Users.AnyAsync(cancellationToken);
}
