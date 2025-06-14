using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Application.Features.Users.Commands.LoginUser;
using UMS.Application.Settings;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, LoginUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGeneratorService _jwtTokenGeneratorService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TokenSettings _tokenSettings;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            IUserRepository userRepository,
            IJwtTokenGeneratorService jwtTokenGeneratorService,
            IUnitOfWork unitOfWork,
            IOptions<TokenSettings> tokenSettings,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _userRepository = userRepository;
            _jwtTokenGeneratorService = jwtTokenGeneratorService;
            _unitOfWork = unitOfWork;
            _tokenSettings = tokenSettings.Value;
            _logger = logger;
        }

        public async Task<Result<LoginUserResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            // 1. Get the principal from expired access token (without validating lifetime)  
            var principal = _jwtTokenGeneratorService.GetPrincipalFromExpiredToken(command.AccessToken);
            if (principal?.FindFirst("uid")?.Value is null)
            {
                return Result.Failure<LoginUserResponse>(new Error(
                    "Token.Invalid",
                    "Invalid access token.",
                    ErrorType.Unauthorized));
            }

            // 2. Get UserId from the principal  
            if (!Guid.TryParse(principal.FindFirst("uid")?.Value, out var userId))
            {
                return Result.Failure<LoginUserResponse>(new Error(
                    "Token.InvalidUserId",
                    "Invalid user identifier in token.",
                    ErrorType.Unauthorized));
            }

            // 3. Get user from repository with refresh tokens
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if(user is null || !user.IsActive || user.IsDeleted)
            {
                return Result.Failure<LoginUserResponse>(new Error(
                    "Token.Invalid",
                    "User not found or is inactive.",
                    ErrorType.Unauthorized));
            }

            // 4. Find the provided refresh token in the user's collection
            var oldRefreshToken = user.RefreshTokens
                .FirstOrDefault(rt => rt.Token == command.RefreshToken);
            if(oldRefreshToken == null || !oldRefreshToken.IsActive)
            {
                // Security measure: If an invalid/ expired/ revoked token is used,
                // it might indicate a compromised token, Revokde all active tokens for this user.
                _logger.LogWarning("Potential security incident: Invalid refresh token used for user {UserId}. Revoking all active tokens.", userId);
                foreach(var token in user.RefreshTokens.Where(rt => rt.IsActive))
                {
                    token.Revoke();
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure<LoginUserResponse>(new Error(
                    "Token.InvalidOrRevoked",
                    "Invalid or revoked refresh token.",
                    ErrorType.Unauthorized));
            }

            // 5. Revoke the old refresh token
            oldRefreshToken.Revoke();
            _logger.LogInformation("Old refresh token for user {UserId} on device {DeviceId} has been revoked.", userId, oldRefreshToken.DeviceId);

            // 6. Generate a new JWT (Access token)
            (string newAccessToken, DateTime newAccessTokenExpiry) = _jwtTokenGeneratorService.GenerateToken(user);

            // 7. Generate a new refresh token (token rotation)
            var refreshTokenValidity = TimeSpan.FromDays(_tokenSettings.RefreshTokenExpiryDays);
            var newRefreshToken = user.AddRefreshToken(oldRefreshToken.DeviceId, refreshTokenValidity);
            await _userRepository.AddRefreshTokenAsync(newRefreshToken, cancellationToken);

            // 8. Persist all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("New access and refresh tokens generated for user {UserId} on device {DeviceId}.", userId, newRefreshToken.DeviceId);

            var response = new LoginUserResponse(
                user.Id,
                user.Email,
                user.UserCode,
                newAccessToken,
                newAccessTokenExpiry,
                newRefreshToken.Token);

            return Result<LoginUserResponse>.Success(response);
        }
    }
}
