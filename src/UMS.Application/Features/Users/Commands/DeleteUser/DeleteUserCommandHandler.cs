using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Application.Settings;
using UMS.Domain.Users;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.DeleteUser
{
    public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly AdminSettings _adminSettings;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ILogger<DeleteUserCommandHandler> logger,
            IOptions<AdminSettings> adminSettings)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
            _adminSettings = adminSettings.Value;
        }

        public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
        {
            var userToDelete = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
            if(userToDelete is null)
            {
                return Result.Success();
            }

            if (userToDelete.IsDeleted)
            {
                return Result.Failure(new Error(
                    "User.AccountDeleted",
                    "This account is unavailable.",
                    ErrorType.Conflict));
            }

            // Prevent deletion of the SuperAdmin account
            if (userToDelete.Email.Equals(_adminSettings.Email, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(new Error(
                    "User.CannotDeleteSuperAdmin",
                    "The SuperAdmin account cannot be deleted.",
                    ErrorType.Conflict));
            }

            // Prevent a user from deleting themselves
            var currentUserId = _currentUserService.UserId;
            if(userToDelete.Id == currentUserId)
            {
                return Result.Failure(new Error(
                    "User.CannotDeleteSelf",
                    "Users cannot delete their own account.",
                    ErrorType.Conflict));
            }

            userToDelete.MarkAsDeleted(currentUserId);
            _logger.LogInformation("User {UserId} marked as deleted by admin {AdminId}", command.UserId, currentUserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
