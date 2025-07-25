using Microsoft.EntityFrameworkCore;
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
    }
}
