using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Queries;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Queries.GetMyProfile
{
    public class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, UserProfileResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetMyProfileQueryHandler> _logger;

        public GetMyProfileQueryHandler(
            ICurrentUserService currentUserService, 
            IUserRepository userRepository, 
            ILogger<GetMyProfileQueryHandler> logger)
        {
            _currentUserService = currentUserService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<UserProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if(currentUserId is null)
            {
                _logger.LogWarning("GetMyProfile failed: User is not authenticated.");
                return Result.Failure<UserProfileResponse>(new Error(
                    "User.NotAuthenticated",
                    "User is not authenticated.",
                    ErrorType.Unauthorized));
            }

            _logger.LogInformation("Fetching profile for authenticated user: {UserId}", currentUserId.Value);

            var user = await _userRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
            if (user == null)
            {
                _logger.LogError("GetMyProfile failed: Authenticated user {UserId} not found in database.", currentUserId.Value);
                // This is a critical error, as an authenticated user should always exist in the DB.
                return Result.Failure<UserProfileResponse>(new Error(
                    "User.NotFoundInDb",
                    "Authenticated user could not be found.",
                    ErrorType.NotFound));
            }

            if (user.IsDeleted)
            {
                return Result.Failure<UserProfileResponse>(new Error(
                    "User.AccountDeleted",
                    "This account is unavailable.",
                    ErrorType.Conflict));
            }

            var response = new UserProfileResponse(
                user.Id,
                user.UserCode,
                user.Email,
                user.FirstName,
                user.LastName,
                user.IsActive,
                user.CreatedAtUtc,
                user.LastLoginAtUtc
            );

            return Result<UserProfileResponse>.Success(response);
        }
    }
}
