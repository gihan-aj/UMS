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
    }
}
