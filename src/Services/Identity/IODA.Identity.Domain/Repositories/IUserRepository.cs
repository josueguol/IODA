namespace IODA.Identity.Domain.Repositories;

/// <summary>
/// Repositorio de usuarios para el Identity Service.
/// </summary>
public interface IUserRepository
{
    Task<Entities.User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Entities.User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Entities.User> AddAsync(Entities.User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
