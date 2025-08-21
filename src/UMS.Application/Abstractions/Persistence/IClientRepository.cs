using System.Threading.Tasks;
using System.Threading;
using UMS.Domain.Clients;
using System;
using UMS.SharedKernel;

namespace UMS.Application.Abstractions.Persistence
{
    public interface IClientRepository
    {
        Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<Client?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken);

        Task AddAsync(Client client, CancellationToken cancellationToken);

        Task<PagedList<Client>> GetPagedListAsync(
            int page,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken);
    }
}
