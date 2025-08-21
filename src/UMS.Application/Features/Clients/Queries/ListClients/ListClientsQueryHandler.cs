using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Queries;
using UMS.SharedKernel;

namespace UMS.Application.Features.Clients.Queries.ListClients
{
    public class ListClientsQueryHandler : IQueryHandler<ListClientsQuery, PagedList<ClientResponse>>
    {
        private readonly IClientRepository _clientRepository;

        public ListClientsQueryHandler(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task<Result<PagedList<ClientResponse>>> Handle(ListClientsQuery request, CancellationToken cancellationToken)
        {
            var pagedClientList = await _clientRepository.GetPagedListAsync(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                cancellationToken);

            var clientResponses = pagedClientList.Items
                .Select(c => new ClientResponse(c.Id, c.ClientId, c.ClientName, c.CreatedAtUtc))
                .ToList();

            var pagedResponse = new PagedList<ClientResponse>(
                clientResponses, 
                pagedClientList.Page, 
                pagedClientList.PageSize,
                pagedClientList.TotalCount);

            return pagedResponse;
        }
    }
}
