using System.Threading.Tasks;
using System.Threading;
using UMS.Domain.Clients;

namespace UMS.Application.Abstractions.Persistence
{
    public interface IClientRepository
    {
        Task<Client?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken);

        Task AddAsync(Client client, CancellationToken cancellationToken);
    }
}
