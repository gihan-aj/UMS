using Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.ActivateUserbyAdmin
{
    public class ActivateUserByAdminCommandHandler : ICommandHandler<ActivateUserByAdminCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ActivateUserByAdminCommandHandler> _logger;

        public ActivateUserByAdminCommandHandler(
            IUserRepository userRepository, 
            IUnitOfWork unitOfWork, 
            ILogger<ActivateUserByAdminCommandHandler> logger, 
            ICurrentUserService currentUserService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(ActivateUserByAdminCommand command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
            if(user is null)
            {
                return Result.Failure(new Error(
                    "User.NotFound",
                    $"User with ID {command.UserId} not found.",
                    ErrorType.NotFound));
            }

            user.Activate(_currentUserService.UserId);
            _logger.LogInformation("User {UserId} activated by admin {AdminId}.", command.UserId, _currentUserService.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
