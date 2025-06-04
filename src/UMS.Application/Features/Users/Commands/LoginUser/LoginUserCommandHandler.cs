using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.LoginUser
{
    public class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, LoginUserResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IJwtTokenGeneratorService _jwtTokenGeneratorService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoginUserCommandHandler> _logger;

        public LoginUserCommandHandler(
            IUserRepository userRepository, 
            IPasswordHasherService passwordHasherService, 
            ILogger<LoginUserCommandHandler> logger, 
            IUnitOfWork unitOfWork, 
            IJwtTokenGeneratorService jwtTokenGeneratorService)
        {
            _userRepository = userRepository;
            _passwordHasherService = passwordHasherService;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _jwtTokenGeneratorService = jwtTokenGeneratorService;
        }

        public async Task<Result<LoginUserResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting login for email: {Email}", command.Email);

            // 1. Retrive user by email
            var user = await _userRepository.GetByEmailAsync(command.Email);
            if(user is null)
            {
                _logger.LogWarning("Login failed: User not found for email {Email}", command.Email);
                return Result.Failure<LoginUserResponse>(new Error(
                    "Auth.InvalidCredentials",
                    "Invalid email or password.", // Generic message for security
                     ErrorType.Unauthorized));
            }

            // 2. Check if account is active (and not deleted - soft delete is handled by repo query filter)
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: Account for email {Email} is not active.", command.Email);
                return Result.Failure<LoginUserResponse>(new Error(
                    "Auth.AccountNotActive",
                    "Your account is not active. Please activate your account or contact support.",
                    ErrorType.Unauthorized));
            }

            // 3. Verify password
            bool isPasswordValid = _passwordHasherService.VerifyPassword(command.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: Invalid password for email {Email}.", command.Email);
                // TODO: Implement account lockout mechanism after several failed attempts
                return Result.Failure<LoginUserResponse>(new Error(
                    "Auth.InvalidCredentials",
                    "Invalid email or password.", // Generic message
                    ErrorType.Unauthorized));
            }

            // 4. Generate JWT Token
            (string token, DateTime expiresAtUtc) = _jwtTokenGeneratorService.GenerateToken(user);
            _logger.LogInformation("JWT token generated for user {UserId}", user.Id);

            // 5. Update last login time (optional, but good practice)
            //    The User entity now has RecordLogin method
            user.RecordLogin(user.Id); // Pass user.Id as modifier for now, or setup ICurrentUserService
            // No need to explicitly call _userRepository.UpdateAsync(user) if using EF Core change tracking
            // and SaveChangesAsync is called by UnitOfWork.

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Last login time updated for user {UserId}", user.Id);

            // 6. Return success response
            var response = new LoginUserResponse(
                user.Id,
                user.Email,
                user.UserCode,
                token,
                expiresAtUtc
            );

            _logger.LogInformation("Login successful for user {UserId}, email {Email}", user.Id, command.Email);
            return Result<LoginUserResponse>.Success(response);
        }
    }
}
