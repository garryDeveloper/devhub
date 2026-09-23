using DevHub.Application.Users;
using DevHub.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(DevHubDbContext dbContext) : IUserRepository
{
    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Users.FindAsync([id], cancellationToken).ConfigureAwait(false);

    public void Add(User user) => dbContext.Users.Add(user);
}
