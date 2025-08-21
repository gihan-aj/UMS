using System.Security.Cryptography;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;
using UMS.Domain.Clients;
using System.Linq;

namespace UMS.Application.Features.Clients.Commands.CreateClient
{
    public class CreateClientCommandHandler : ICommandHandler<CreateClientCommand, CreateClientResponse>
    {
        private readonly IClientRepository _clientRepository;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateClientCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public CreateClientCommandHandler(
            IClientRepository clientRepository,
            IPasswordHasherService passwordHasherService,
            IUnitOfWork unitOfWork,
            ILogger<CreateClientCommandHandler> logger,
            ICurrentUserService currentUserService)
        {
            _clientRepository = clientRepository;
            _passwordHasherService = passwordHasherService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<CreateClientResponse>> Handle(CreateClientCommand request, CancellationToken cancellationToken)
        {
            var existingClient = await _clientRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
            if (existingClient != null)
            {
                return Result.Failure<CreateClientResponse>(
                    new Error(
                        "Client.AlreadyExists", 
                        "A client with this Client ID already exists.", 
                        ErrorType.Conflict));
            }

            // 1.Generate a secure, random client secret
            var clientSecret = GenerateRandomSecret();

            // 2. Hash the secret for storage
            var clientSecretHash = _passwordHasherService.HashPassword(clientSecret);

            // 3. Create the domain entity
            var createdUserId = _currentUserService.UserId;
            var newClient = Client.Create(request.ClientId, request.ClientName, clientSecretHash, createdUserId);

            // 4. Add redirect URIs
            newClient.AddRedirectUris(request.RedirectUris);

            await _clientRepository.AddAsync(newClient, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("New client application '{ClientName}' ({ClientId}) created.", newClient.ClientName, newClient.ClientId);

            // 5. Return the raw, unhashed secret in the response
            var response = new CreateClientResponse(newClient.Id, newClient.ClientId, newClient.ClientName, clientSecret);
            return response;
        }

        private string GenerateRandomSecret(int length = 32)
        {
            // Generate a cryptographically strong, URL-safe secret
            var randomBytes = RandomNumberGenerator.GetBytes(length);
            return Convert.ToBase64String(randomBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
