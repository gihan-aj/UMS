using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Domain.Users;

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
                .FirstOrDefaultAsync(u => u.Email.ToLower() == lowerEmail);
        }

        // You might add other methods here as needed, for example:
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Users.FindAsync(id); // FindAsync respects query filters if the entity is not already tracked.
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
    }
}
