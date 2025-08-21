using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;

namespace UMS.Application.Features.Clients.Commands.UpdateClient
{
    public class UpdateClientCommandHandler : ICommandHandler<UpdateClientCommand>
    {
        private readonly IClientRepository _clientRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateClientCommandHandler> _logger;

        public UpdateClientCommandHandler(
            IClientRepository clientRepository, 
            IUnitOfWork unitOfWork, 
            ICurrentUserService currentUserService, 
            ILogger<UpdateClientCommandHandler> logger)
        {
            _clientRepository = clientRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result> Handle(UpdateClientCommand command, CancellationToken cancellationToken)
        {
            var client = await _clientRepository.GetByIdAsync(command.Id, cancellationToken);
            if(client is null)
            {
                return Result.Failure(new Error(
                    "Client.NotFound",
                    $"Client with id {command.Id} not found.",
                    ErrorType.NotFound));
            }

            var modifiedBy = _currentUserService.UserId;
            client.Update(command.ClientName, modifiedBy);

            var existingUris = client.RedirectUris.Select(ru => ru.Uri).ToHashSet();
            var requestedUris = command.RedirectUris.ToHashSet();
            
            var urisToAdd = requestedUris.Except(existingUris);
            var urisToRemove = client.RedirectUris.Where(ru => !requestedUris.Contains(ru.Uri)).ToList();

            if (urisToRemove.Any())
            {
                _clientRepository.RemoveRedirectUris(urisToRemove);
            }

            if (urisToAdd.Any())
            {
                client.AddRedirectUris(urisToAdd.ToList());
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Client {ClientId} has been updated.", client.ClientId);

            return Result.Success();
        }
    }
}
