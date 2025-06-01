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

        public Task<Result<LoginUserResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
