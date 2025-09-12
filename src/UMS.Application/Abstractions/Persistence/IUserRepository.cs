using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UMS.Domain.Users;
using UMS.SharedKernel;

namespace UMS.Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);

        Task AddAsync(User user);

        Task<bool> ExistsByEmailAsync(string email);

        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<User?> GetByIdWithRolesAndPermissionsAsync(Guid id, CancellationToken cancellationToken);

        Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

        Task<User?> GetUserByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

        Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken);

        Task<PagedList<User>> GetPagedListAsync(
            int page,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken);

        Task<PagedList<User>> ListAsync(PaginationQuery query, CancellationToken cancellationToken = default);

        void RemoveUserRolesRange(List<UserRole> userRoles);

        Task AddUserRolesRangeAsync(List<UserRole> userRoles, CancellationToken cancellationToken = default);
    }
}
