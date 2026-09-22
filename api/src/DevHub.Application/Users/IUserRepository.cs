using DevHub.Domain.Users;

namespace DevHub.Application.Users;

/// <summary>
/// Loads and stages <see cref="User"/> aggregates. Staging only — nothing is written until the
/// handler calls <see cref="Common.IUnitOfWork.SaveChangesAsync"/>.
/// </summary>
/// <remarks>
/// Every method takes an email already passed through <see cref="User.NormalizeEmail"/>. The
/// column is citext, so the database would match either way, but normalizing first keeps the
/// behaviour independent of that column type.
/// </remarks>
public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    void Add(User user);
}
