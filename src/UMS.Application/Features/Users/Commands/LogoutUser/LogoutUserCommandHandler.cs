using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.LogoutUser
{
    public class LogoutUserCommandHandler : ICommandHandler<LogoutUserCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LogoutUserCommandHandler> _logger;

        public LogoutUserCommandHandler(
            IUserRepository userRepository, 
            IUnitOfWork unitOfWork, 
            ILogger<LogoutUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(LogoutUserCommand command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByRefreshTokenAsync(command.RefreshToken, cancellationToken);
            // If no user or token is found, or if the token is already inactive,
            // we can consider the logout successful from the server's perspective.
            if (user is null)
            {
                _logger.LogWarning("Logout attempted with an invalid or non-existent refresh token.");
                return Result.Success();
            }

            var refreshTokenToRevoke = user.RefreshTokens.FirstOrDefault(rt => rt.Token == command.RefreshToken);
            if(refreshTokenToRevoke is null || !refreshTokenToRevoke.IsActive)
            {
                _logger.LogInformation("Logout successful for user {UserId} (token was already inactive).", user.Id);
                return Result.Success();
            }

            refreshTokenToRevoke.Revoke();
            _logger.LogInformation("Revoking refresh token for user {UserId} on device {DeviceId}.", user.Id, refreshTokenToRevoke.DeviceId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Logout successful for user {UserId}, token has been revoked.", user.Id);
            return Result.Success();
        }
    }
}
