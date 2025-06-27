using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Domain.Users;
using UMS.SharedKernel;

namespace UMS.Infrastructure.Persistence.Repositories
{
    public class InMemoryUserRepository : IUserRepository
    {
        // Static list to act as our in-memory data store.
        // In a real DI setup with singleton lifetime for this repo, static might not be needed,
        // but for simplicity and ensuring data persists across requests (if repo is transient/scoped),
        // static makes it behave more like a shared store.
        private static readonly List<User> _users = new List<User>();
        private static readonly object _lock = new object(); // For basic thread safety

        public Task AddAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            lock (_lock)
            {
                // Simulate a primary key constraint / uniqueness for ID if needed, though GUIDs are generally unique.
                if (_users.Any(u => u.Id == user.Id))
                {
                    // This scenario should ideally be rare with GUIDs.
                    // In a real DB, this would be a PK violation.
                    throw new InvalidOperationException($"User with ID {user.Id} already exists in the in-memory store.");
                }
                // Email uniqueness is checked by the handler before calling AddAsync.
                _users.Add(user);
            }
            return Task.CompletedTask;
        }

        public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AddUserRolesRangeAsync(List<UserRole> userRoles, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsByEmailAsync(string email)
        {
            bool exists;
            lock (_lock)
            {
                exists = _users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult(exists);
        }

        public Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            User? user;
            lock (_lock)
            {
                user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult(user);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<PagedList<User>> GetPagedListAsync(int page, int pageSize, string? searchTerm, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<User?> GetUserByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void RemoveUserRolesRange(List<UserRole> userRoles)
        {
            throw new NotImplementedException();
        }
    }
}
