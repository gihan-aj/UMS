using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Queries;
using UMS.SharedKernel;

namespace UMS.Application.Features.Clients.Queries.GetClientById
{
    public class GetClientByIdQueryHandler : IQueryHandler<GetClientByIdQuery, ClientDetailsResponse>
    {
        private readonly IClientRepository _clientRepository;

        public GetClientByIdQueryHandler(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task<Result<ClientDetailsResponse>> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
        {
            var client = await _clientRepository.GetByIdAsync(request.Id, cancellationToken);
            if (client == null)
            {
                return Result.Failure<ClientDetailsResponse>(
                    new Error(
                        "Client.NotFound", 
                        "Client not found.", 
                        ErrorType.NotFound));
            }

            var response = new ClientDetailsResponse(
                client.Id,
                client.ClientId,
                client.ClientName,
                client.RedirectUris.Select(ru => ru.Uri).ToList(),
                client.CreatedAtUtc
            );

            return response;
        }
    }
}
