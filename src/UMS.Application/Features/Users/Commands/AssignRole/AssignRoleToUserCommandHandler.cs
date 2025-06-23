using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.AssignRole
{
    public class AssignRoleToUserCommandHandler : ICommandHandler<AssignRoleToUserCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignRoleToUserCommandHandler> _logger;

        public AssignRoleToUserCommandHandler(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<AssignRoleToUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure(new Error(
                    "User.NotFound", 
                    $"User with ID {request.UserId} not found.", 
                    ErrorType.NotFound));
            }

            var role = await _roleRepository.GetByIdAsync(request.RoleId);
            if (role == null)
            {
                return Result.Failure(new Error(
                    "Role.NotFound", 
                    $"Role with ID {request.RoleId} not found.", 
                    ErrorType.NotFound));
            }

            // Use the domain method to assign the role
            // The method already checks for duplicates.
            var assigningUserId = _currentUserService.UserId ?? Guid.Empty;
            user.AssignRole(request.RoleId, assigningUserId);

            _logger.LogInformation("Assigning role {RoleId} to user {UserId} by {AssigningUserId}.",
                request.RoleId, request.UserId, assigningUserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
