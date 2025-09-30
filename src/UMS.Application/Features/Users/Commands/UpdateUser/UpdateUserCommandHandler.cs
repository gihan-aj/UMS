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

namespace UMS.Application.Features.Users.Commands.UpdateUser
{
    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly AdminSettings _adminSettings;
        private readonly ILogger<UpdateUserCommandHandler> _logger;


        public UpdateUserCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateUserCommandHandler> logger,
            ICurrentUserService currentUserService,
            IOptions<AdminSettings> adminSettings)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserService = currentUserService;
            _adminSettings = adminSettings.Value;
        }

        public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
        {
            var userToUpdate = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
            if(userToUpdate is null)
            {
                return Result.Failure(new Error(
                    "User.NotFound",
                    $"User with ID {command.UserId} not found.",
                    ErrorType.NotFound));
            }

            if (userToUpdate.IsDeleted)
            {
                return Result.Failure(new Error(
                    "User.AccountDeleted",
                    "This account is unavailable.",
                    ErrorType.Conflict));
            }

            if (userToUpdate.Email.Equals(_adminSettings.Email, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(new Error(
                    "User.CannotEditSuperAdmin",
                    "The SuperAdmin account cannot be modified.",
                    ErrorType.Conflict));
            }

            var modifyingUserId = _currentUserService.UserId;

            userToUpdate.UpdateProfile(
                command.FirstName,
                command.LastName,
                modifyingUserId);

            _logger.LogInformation("Updating profile for user {UserId} by admin {AdminId}", command.UserId, modifyingUserId);

            // No explicit repository.Update() needed due to EF Core change tracking
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
