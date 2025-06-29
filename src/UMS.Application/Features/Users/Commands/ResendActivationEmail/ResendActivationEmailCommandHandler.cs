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
            if(user is null)
            {
                _logger.LogWarning("Resend activation failed: User not found for email {Email}.", command.Email);
                // It's often better not to reveal if an email exists or not for this kind of endpoint
                // for security reasons (to prevent email enumeration).
                // So, we can return a generic success-like message or a specific "if your email is registered..."
                // For simplicity and consistency with other errors for now:
                return Result.Failure(new Error(
                    "User.NotFound",
                    "If an account with this email exists and requires activation, a new email has been sent.",
                    ErrorType.NotFound)); // Or ErrorType.Validation if considering the input potentially invalid
            }

            if (user.IsActive)
            {
                _logger.LogInformation("Account for email {Email} is already active. No activation email resent.", command.Email);
                return Result.Success(); // Account is already active, consider this a success.
            }

            if (user.IsDeleted)
            {
                _logger.LogWarning("Resend activation failed: Account for email {Email} is deleted.", command.Email);
                return Result.Failure(new Error(
                   "User.Deleted",
                   "This account has been deleted and cannot be activated.",
                   ErrorType.Conflict));
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
