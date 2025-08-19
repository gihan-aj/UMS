using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Domain.Authorization;

namespace UMS.Infrastructure.Persistence.Repositories
{
    public class EfCorePermissionRepository : IPermissionRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public EfCorePermissionRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Permission>> GetPermissionsByNameRangeAsync(List<string> permissionNames, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Permissions
                .Where(p => permissionNames.Contains(p.Name))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Permission>> GetPermissionsByClientIdAsync(Guid ClientId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Permissions
                .Where(p => p.ClientId == ClientId)
                .ToListAsync(cancellationToken);
        }

        public async Task<short> GetTheLastId(CancellationToken cancellationToken = default)
        {
            short lastId = await _dbContext.Permissions.AnyAsync(cancellationToken)
                    ? (await _dbContext.Permissions.MaxAsync(p => p.Id, cancellationToken))
                    : (short)0;

            return lastId;
        }

        public async Task AddRangeAsync(List<Permission> permissions, CancellationToken cancellationToken = default)
        {
            await _dbContext.Permissions.AddRangeAsync(permissions, cancellationToken);
        }
    }
}
