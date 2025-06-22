using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UMS.Application.Abstractions.Persistence;
using UMS.Domain.Authorization;
using UMS.SharedKernel;

namespace UMS.Infrastructure.Persistence.Repositories
{
    public class EfCoreRoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public EfCoreRoleRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        }

        public async Task<PagedList<Role>> GetPagedListAsync(
            int page, 
            int pageSize, 
            string? searchTerm, 
            CancellationToken cancellationToken = default)
        {
            IQueryable<Role> query = _dbContext.Roles;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.Name.Contains(searchTerm));
            }

            return await PagedList<Role>.CreateAsync(query, page, pageSize, cancellationToken);
        }

        public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        {
            await _dbContext.Roles.AddAsync(role, cancellationToken);
        }

        public void Update(Role role)
        {
            _dbContext.Roles.Update(role);
        }

        public async Task<Role?> GetByIdWithPermissionsAsync(byte id, CancellationToken cancellationToken = default)
        {
            // Eagerly load the RolePermissions join entity, and then the associated Permission entity
            return await _dbContext.Roles
                .Include(r => r.Permissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<byte> GetNextIdAsync()
        {
            // A simple approach to get the next ID. This is NOT concurrency-safe without transaction isolation.
            // For low-frequency role creation, this is often acceptable.
            // For high concurrency, a database sequence would be better.
            byte maxId = await _dbContext.Roles.AnyAsync()
                ? await _dbContext.Roles.MaxAsync(r => r.Id)
                : (byte)0;

            return (byte)(maxId + 1);
        }

        public async Task<Role?> GetByIdAsync(byte id)
        {
            return await _dbContext.Roles
                .FindAsync(id);
        }

        public async Task<bool> IsRoleAssignedToUsersAsync(byte roleId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.UserRoles.AnyAsync(ur => ur.RoleId == roleId, cancellationToken);
        }

        public void RemoveRolePermissionsRange(List<RolePermission> rolePermissionsToRemove)
        {
            _dbContext.RolePermissions.RemoveRange(rolePermissionsToRemove);
        }
        
        public async Task AddRolePermissionsRangeAsync(List<RolePermission> rolePermissionsToRemove, CancellationToken cancellationToken = default)
        {
            await _dbContext.RolePermissions.AddRangeAsync(rolePermissionsToRemove, cancellationToken);
        }

        public async Task<List<short>> GetExistingPermissionsAsync(List<short> permissionIds, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Permissions
                .Where(p => permissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
