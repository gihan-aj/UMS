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

        public async Task<Role?> GetByNameAsync(string name)
        {
            return await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == name);
        }

        public async Task<PagedList<Role>> GetPagedListAsync(
            int page, 
            int pageSize, 
            string? searchTerm, 
            CancellationToken cancellationToken)
        {
            IQueryable<Role> query = _dbContext.Roles;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.Name.Contains(searchTerm));
            }

            return await PagedList<Role>.CreateAsync(query, page, pageSize, cancellationToken);
        }
    }
}
