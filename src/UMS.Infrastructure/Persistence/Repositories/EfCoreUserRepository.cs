using Microsoft.EntityFrameworkCore;
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
    public class EfCoreUserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public EfCoreUserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext)); ;
        }

        public async Task AddAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await _dbContext.Users.AddAsync(user);
            // Note: SaveChangesAsync is typically called by a Unit of Work pattern or
            // at the end of a command handler after all operations are complete.
            // For simplicity here, we might assume it's called elsewhere,
            // or if this repository is the sole actor, it could call it.
            // For now, we'll assume SaveChangesAsync is handled by the caller/UnitOfWork.
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }
            // Convert both sides to lower case for case-insensitive comparison.
            string lowerEmail = email.ToLowerInvariant();
            return await _dbContext.Users
                .AnyAsync(u => u.Email.ToLower() == lowerEmail);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null; // Or throw ArgumentNullException based on contract preference
            }

            // The .Users DbSet will automatically apply the HasQueryFilter(u => !u.IsDeleted)
            string lowerEmail = email.ToLowerInvariant();
            return await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == lowerEmail);
        }

        // You might add other methods here as needed, for example:
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken); 
            // FindAsync respects query filters if the entity is not already tracked.
            // If tracked and soft-deleted, it might return it.
            // A safer bet for GetById that respects soft delete:
            // return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task UpdateAsync(User user)
        {
            _dbContext.Users.Update(user);
            // Again, SaveChangesAsync would be called by a Unit of Work or handler.
            await Task.CompletedTask;
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        }

        public async Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken)
        {
            var users = await _dbContext.Users
                .ToListAsync(cancellationToken);

            return users;
        }

        public async Task<PagedList<User>> GetPagedListAsync(
            int page,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken)
        {
            IQueryable<User> query = _dbContext.Users;

            // Apply search filter if a search term is provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u =>
                    u.Email.Contains(searchTerm) ||
                    (u.FirstName != null && u.FirstName.Contains(searchTerm)) ||
                    (u.LastName != null && u.LastName.Contains(searchTerm)) ||
                    u.UserCode.Contains(searchTerm)
                );
            }

            return await PagedList<User>.CreateAsync(query, page, pageSize, cancellationToken);
        }

        public void RemoveUserRolesRange(List<UserRole> userRoles)
        {
            _dbContext.UserRoles.RemoveRange(userRoles);
        }

        public async Task AddUserRolesRangeAsync(List<UserRole> userRoles, CancellationToken cancellationToken = default)
        {
            await _dbContext.UserRoles.AddRangeAsync(userRoles, cancellationToken);
        }
    }
}
