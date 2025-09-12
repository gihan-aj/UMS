using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
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
                .AsSplitQuery() // Improves performance
                .FirstOrDefaultAsync(u => u.Email.ToLower() == lowerEmail);
        }

        // You might add other methods here as needed, for example:
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken); 
            // FindAsync respects query filters if the entity is not already tracked.
            // If tracked and soft-deleted, it might return it.
            // A safer bet for GetById that respects soft delete:
            // return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByIdWithRolesAndPermissionsAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.Permissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
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

        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var token = await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.RefreshTokens)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

            return token?.User;
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

        public async Task<PagedList<User>> ListAsync(PaginationQuery query, CancellationToken cancellationToken = default)
        {
            IQueryable<User> usersQuery = _dbContext.Users.AsQueryable();

            // Apply Filters
            if(query.Filters != null && query.Filters.Any())
            {
                usersQuery = ApplyFilters(usersQuery, query.Filters);
            }

            // Apply Sorting
            if (!string.IsNullOrWhiteSpace(query.SortColumn))
            {
                // Note: The property names must match the User entity's property names.
                // For security, the sortColumn can be validated against a list of allowed columns.
                var sortOrder = query.SortOrder?.ToLower() == "desc" ? "descending" : "ascending";
                usersQuery = usersQuery.OrderBy($"{query.SortColumn} {sortOrder}");
            }
            else
            {
                // Default sort order
                usersQuery = usersQuery.OrderBy(u => u.FirstName);
            }

            return await PagedList<User>.CreateAsync(usersQuery, query.Page, query.PageSize, cancellationToken);
        }

        public void RemoveUserRolesRange(List<UserRole> userRoles)
        {
            _dbContext.UserRoles.RemoveRange(userRoles);
        }

        public async Task AddUserRolesRangeAsync(List<UserRole> userRoles, CancellationToken cancellationToken = default)
        {
            await _dbContext.UserRoles.AddRangeAsync(userRoles, cancellationToken);
        }

        private IQueryable<User> ApplyFilters(IQueryable<User> query, List<Filter> filters)
        {
            if (filters == null || !filters.Any())
            {
                return query;
            }

            var whereClause = new StringBuilder();
            var parameters = new List<object>();
            var paramIndex = 0;

            foreach (var filter in filters)
            {
                if (string.IsNullOrWhiteSpace(filter.ColumnName) || string.IsNullOrWhiteSpace(filter.Value))
                {
                    continue;
                }

                if( whereClause.Length > 0)
                {
                    whereClause.Append(" AND ");
                }

                // Again, for security, validate ColumnName against allowed properties.
                string propertyName = filter.ColumnName;
                string value = filter.Value;

                switch (filter.Operator?.ToLower())
                {
                    case "contains":
                        whereClause.Append($"{propertyName}.ToLower().Contains(@{paramIndex})");
                        parameters.Add(value.ToLower());
                        break;

                    case "equals":
                        whereClause.Append($"{propertyName} == @{paramIndex}");
                        parameters.Add(value); // Needs conversion for non-string types
                        break;

                    case "notequals":
                        whereClause.Append($"{propertyName} != @{paramIndex}");
                        parameters.Add(value);
                        break;

                    case "gt": // Greater Than
                        whereClause.Append($"{propertyName} > @{paramIndex}");
                        parameters.Add(value);
                        break;

                    case "gte": // Greater Than or Equal To
                        whereClause.Append($"{propertyName} >= @{paramIndex}");
                        parameters.Add(value);
                        break;

                    case "lt": // Less Than
                        whereClause.Append($"{propertyName} < @{paramIndex}");
                        parameters.Add(value);
                        break;

                    case "lte": // Less Than or Equal To
                        whereClause.Append($"{propertyName} <= @{paramIndex}");
                        parameters.Add(value);
                        break;

                    default:
                        // Default to contains for safety
                        whereClause.Append($"{propertyName}.ToLower().Contains(@{paramIndex})");
                        parameters.Add(value.ToLower());
                        break;
                }

                paramIndex++;
            }

            if(whereClause.Length > 0)
            {
                return query.Where(whereClause.ToString(), parameters.ToArray());
            }

            return query;
        }
    }
}
