using UMS.Application.Common.Messaging.Queries;
using UMS.SharedKernel;

namespace UMS.Application.Features.Clients.Queries.ListClients
{
    public sealed record ListClientsQuery(
        int Page,
        int PageSize,
        string? SearchTerm) : IQuery<PagedList<ClientResponse>>;
}
