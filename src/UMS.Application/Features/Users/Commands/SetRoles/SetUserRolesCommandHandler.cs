using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Commands;
using UMS.Domain.Users;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.SetRoles
{
    public class SetUserRolesCommandHandler : ICommandHandler<SetUserRolesCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SetUserRolesCommandHandler> _logger;

        public SetUserRolesCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<SetUserRolesCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(SetUserRolesCommand command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
            if(user is null)
            {
                return Result.Failure(new Error(
                    "User.NotFound",
                    $"User with ID {command.UserId} not found.",
                    ErrorType.NotFound));
            }

            // Prevent modification of the SuperAdmin's roles
            if (user.Email.Equals("superadmin@ums.local", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(new Error(
                    "User.CannotModifySuperAdmin",
                    "The SuperAdmin user's roles cannot be changed.",
                    ErrorType.Conflict));
            }

            var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
            var requestedRoleIds = command.RoleIds.ToHashSet();

            var roleIdsToAdd = requestedRoleIds.Except(currentRoleIds).ToList();
            var roleIdsToRemove = currentRoleIds.Except(requestedRoleIds).ToList();

            if (roleIdsToRemove.Any())
            {
                var userRolesToRemove = user.UserRoles
                    .Where(ur => roleIdsToRemove.Contains(ur.RoleId))
                    .ToList();

                _userRepository.RemoveUserRolesRange(userRolesToRemove);
                _logger.LogInformation("Removing {Count} roles from user {UserId}.", userRolesToRemove.Count, user.Id);
            }

            if(roleIdsToAdd.Any())
            {
                var newRoles = roleIdsToAdd.Select(roleId => new UserRole { UserId = user.Id, RoleId = roleId }).ToList();
                await _userRepository.AddUserRolesRangeAsync(newRoles, cancellationToken);
                _logger.LogInformation("Adding {Count} roles to user {UserId}.", newRoles.Count, user.Id);
            }

            if (roleIdsToRemove.Any() || roleIdsToAdd.Any())
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
    }
}
