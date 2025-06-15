using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UMS.Domain.Users;

namespace UMS.Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);

        Task AddAsync(User user);

        Task<bool> ExistsByEmailAsync(string email);

        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

        Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken);
    }
}
