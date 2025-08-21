using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UMS.Application.Abstractions.Persistence;
using UMS.Domain.Clients;
using UMS.SharedKernel;

namespace UMS.Infrastructure.Persistence.Repositories
{
    public class EfCoreClientRepository : IClientRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public EfCoreClientRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _dbContext.Clients
                .Include(c => c.RedirectUris)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
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

        public async Task<PagedList<Client>> GetPagedListAsync(int page, int pageSize, string? searchTerm, CancellationToken cancellationToken)
        {
            IQueryable<Client> query = _dbContext.Clients;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.ClientId.Contains(searchTerm) || c.ClientName.Contains(searchTerm));
            }

            return await PagedList<Client>.CreateAsync(
                query.OrderBy(c => c.ClientName), 
                page, 
                pageSize, 
                cancellationToken);
        }
    }
}
