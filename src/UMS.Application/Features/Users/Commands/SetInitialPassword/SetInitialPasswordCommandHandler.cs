using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.SetInitialPassword
{
    public class SetInitialPasswordCommandHandler : ICommandHandler<SetInitialPasswordCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SetInitialPasswordCommandHandler> _logger;

        public SetInitialPasswordCommandHandler(
            IUserRepository userRepository,
            IPasswordHasherService passwordHasher,
            IUnitOfWork unitOfWork,
            ILogger<SetInitialPasswordCommandHandler> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<Result> Handle(SetInitialPasswordCommand command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(command.Email);
            if(user is null || !user.ValidateActivationToken(command.Token))
            {
                return Result.Failure(new Error(
                    "User.InvalidToken",
                    "The activation token is invalid or has expired.",
                    ErrorType.Validation));
            }

            if (user.IsActive)
            {
                return Result.Failure(new Error(
                    "User.AlreadyActive", 
                    "This account has already been activated.", 
                    ErrorType.Conflict));
            }

            var passwordHash = _passwordHasher.HashPassword(command.NewPassword);
            user.ChangePassword(passwordHash, user.Id);
            user.Activate(user.Id);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User {UserId} has set their initial password and activated their account.", user.Id);

            return Result.Success();
        }
    }
}
