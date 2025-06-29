using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Commands;
using UMS.Application.Settings;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.RequestPasswordReset
{
    public class RequestPasswordResetCommandHandler : ICommandHandler<RequestPasswordResetCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TokenSettings _tokenSettings;
        private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

        public RequestPasswordResetCommandHandler(
            IUserRepository userRepository, 
            IUnitOfWork unitOfWork, 
            IOptions<TokenSettings> tokenSettings,
            ILogger<RequestPasswordResetCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _tokenSettings = tokenSettings.Value;
            _logger = logger;
        }

        public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing password reset request for email: {Email}", command.Email);

            var user = await _userRepository.GetByEmailAsync(command.Email);

            // For security, do not reveal if an email address is registered or not.
            // Always return a success-like response, but only perform actions if the user exists and is valid.
            if (user is null || !user.IsActive || user.IsDeleted )
            {
                _logger.LogWarning("Password reset request for non-existent, deleted, or inactive email: {Email}. Responding with generic success to prevent enumeration.", command.Email);
                return Result.Success(); // Do nothing, but don't tell the client.
            }

            // The GeneratePasswordResetToken method now raises the domain event
            user.GeneratePasswordResetToken(_tokenSettings.PasswordResetTokenExpiryMinutes);
            _logger.LogInformation("Generated password reset token for user {UserId}", user.Id);

            // The interceptor will publish the UserPasswordResetRequestedEvent after this succeeds.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
