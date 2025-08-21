using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;

namespace UMS.Application.Features.Clients.Commands.DeleteClient
{
    public class DeleteClientCommandHandler : ICommandHandler<DeleteClientCommand>
    {
        private readonly IClientRepository _clientRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DeleteClientCommandHandler> _logger;

        public DeleteClientCommandHandler(
            IClientRepository clientRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ILogger<DeleteClientCommandHandler> logger)
        {
            _clientRepository = clientRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
        {
            var client = await _clientRepository.GetByIdAsync(request.Id, cancellationToken);
            if (client == null)
            {
                return Result.Success(); // Idempotent
            }

            client.MarkAsDeleted(_currentUserService.UserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Client {ClientId} marked as deleted by user {UserId}.", request.Id, _currentUserService.UserId);

            return Result.Success();
        }
    }
}
