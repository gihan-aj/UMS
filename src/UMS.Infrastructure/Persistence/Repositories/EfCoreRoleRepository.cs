using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UMS.Application.Abstractions.Persistence;
using UMS.Domain.Authorization;

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
    }
}
