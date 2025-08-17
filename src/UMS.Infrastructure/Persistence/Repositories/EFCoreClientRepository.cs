using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UMS.Application.Abstractions.Persistence;
using UMS.Domain.Clients;

namespace UMS.Infrastructure.Persistence.Repositories
{
    public class EFCoreClientRepository : IClientRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public EFCoreClientRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Client?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken)
        {
            return await _dbContext.Clients
                .Include(c => c.RedirectUris)
                .FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
        }

        public async Task AddAsync(Client client, CancellationToken cancellationToken)
        {
            await _dbContext.Clients.AddAsync(client, cancellationToken);
        }
    }
}
