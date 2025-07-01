using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Commands;
using UMS.Application.Settings;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.ResendActivationEmail
{
    public class ResendActivationEmailCommandHandler : ICommandHandler<ResendActivationEmailCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ResendActivationEmailCommandHandler> _logger;
        private readonly TokenSettings _tokenSettings;

        public ResendActivationEmailCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IOptions<TokenSettings> tokenSettings,
            ILogger<ResendActivationEmailCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _tokenSettings = tokenSettings.Value;
            _logger = logger;
        }

        public async Task<Result> Handle(ResendActivationEmailCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to resend activation email for: {Email}", command.Email);

            var user = await _userRepository.GetByEmailAsync(command.Email);
            if(user == null || user.IsActive || user.IsDeleted || user.ActivationToken == null)
            {
                _logger.LogWarning("Resend activation requested for invalid user state: {Email}. Responding with generic success.", command.Email);
                return Result.Success(); // Always return success to prevent email enumeration.
            }

            // The RegenerateActivationToken method now raises the domain event
            user.RegenerateActivationToken(_tokenSettings.ActivationTokenExpiryHours);
            _logger.LogInformation("New activation token generated for user {UserId}", user.Id);

            // The interceptor will publish the UserActivationTokenRegeneratedEvent after this succeeds.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
