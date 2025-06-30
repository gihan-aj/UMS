using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Application.Settings;
using UMS.SharedKernel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace UMS.Application.Features.Users.Commands.UpdateMyProfile
{
    public class UpdateMyProfileCommandHandler : ICommandHandler<UpdateMyProfileCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateMyProfileCommandHandler> _logger;

        public UpdateMyProfileCommandHandler(
            IUserRepository userRepository, 
            ICurrentUserService currentUserService, 
            ILogger<UpdateMyProfileCommandHandler> logger, 
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _currentUserService = currentUserService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if(currentUserId is null)
            {
                return Result.Failure(new Error(
                    "User.NotAuthenticated",
                    "User is not authenticated.",
                    ErrorType.Unauthorized));
            }

            var userToUpdate = await _userRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
            if (userToUpdate is null)
            {
                // This would be a critical data integrity issue if an authenticated user doesn't exist in the DB.
                _logger.LogError("Authenticated user with ID {UserId} not found in database.", currentUserId.Value);
                return Result.Failure(new Error(
                    "User.NotFound",
                    "Authenticated user could not be found.",
                    ErrorType.NotFound));
            }

            userToUpdate.UpdateProfile(
                command.FirstName,
                command.LastName,
                currentUserId);

            _logger.LogInformation("User {UserId} is updating their own profile.", currentUserId.Value);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
