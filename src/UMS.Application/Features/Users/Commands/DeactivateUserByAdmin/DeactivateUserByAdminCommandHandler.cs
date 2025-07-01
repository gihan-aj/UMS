using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;
using UMS.Application.Settings;
using Microsoft.Extensions.Options;
using System;

namespace UMS.Application.Features.Users.Commands.DeactivateUserByAdmin
{
    public class DeactivateUserByAdminCommandHandler : ICommandHandler<DeactivateUserByAdminCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly AdminSettings _adminSettings;
        private readonly ILogger<DeactivateUserByAdminCommandHandler> _logger;

        public DeactivateUserByAdminCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeactivateUserByAdminCommandHandler> logger,
            ICurrentUserService currentUserService,
            IOptions<AdminSettings> adminSettings)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserService = currentUserService;
            _adminSettings = adminSettings.Value;
        }

        public async Task<Result> Handle(DeactivateUserByAdminCommand command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(new Error(
                    "User.NotFound",
                    $"User with ID {command.UserId} not found.",
                    ErrorType.NotFound));
            }

            if (user.Email.Equals(_adminSettings.Email, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(new Error(
                    "User.CannotDeactivateSuperAdmin", 
                    "The SuperAdmin account cannot be deactivated.", 
                    ErrorType.Conflict));
            }

            user.Deactivate(_currentUserService.UserId);
            _logger.LogInformation("User {UserId} deactivated by admin {AdminId}.", command.UserId, _currentUserService.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
