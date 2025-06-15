using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Features.Users.Queries.GetMyProfile;
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

        Task<List<UserProfileResponse>> GetAllUsersAsync(CancellationToken cancellationToken);
    }
}
