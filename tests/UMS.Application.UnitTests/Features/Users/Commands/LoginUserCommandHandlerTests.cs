using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Features.Users.Commands.LoginUser;
using UMS.Application.Settings;
using UMS.Domain.Users;

namespace UMS.Application.UnitTests.Features.Users.Commands
{
    public class LoginUserCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IPasswordHasherService> _mockPasswordHasherService;
        private readonly Mock<IJwtTokenGeneratorService> _mockJwtTokenGeneratorService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IOptions<TokenSettings>> _mockTokenSettings;

        private readonly LoginUserCommandHandler _handler;

        public LoginUserCommandHandlerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockPasswordHasherService = new Mock<IPasswordHasherService>();
            _mockJwtTokenGeneratorService = new Mock<IJwtTokenGeneratorService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTokenSettings = new Mock<IOptions<TokenSettings>>();
            var mockLogger = new Mock<ILogger<LoginUserCommandHandler>>();

            // Setup the mock for token settings to return a default value
            _mockTokenSettings.Setup(s => s.Value).Returns(new TokenSettings());

            _handler = new LoginUserCommandHandler(
                _mockUserRepository.Object,
                _mockPasswordHasherService.Object,
                mockLogger.Object,
                _mockUnitOfWork.Object,
                _mockTokenSettings.Object,
                _mockJwtTokenGeneratorService.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenUserNotFound()
        {
            // Arrabge
            var command = new LoginUserCommand("test@example.com", "password", "device123");

            // Simulate the user not being found in the repository
            _mockUserRepository
                .Setup(r => r.GetByEmailAsync(command.Email))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Auth.InvalidCredentials");
        }

        [Fact]
        public async Task Handler_Should_ReturnFailure_WhenPasswordIsInvalid()
        {
            // Arrange
            var command = new LoginUserCommand("test@example.com", "wrong-password", "device123");

            // Create a fake user object for the test
            var user = User.RegisterNew("USR-20250702-00001", command.Email, "hashed-password", "test", "user", 1, null);
            user.Activate(null);

            // Simulate finding the user
            _mockUserRepository
                .Setup(r => r.GetByEmailAsync(command.Email))
                .ReturnsAsync(user);

            // Simulate the password verification failing
            _mockPasswordHasherService
                .Setup(p => p.VerifyPassword(command.Password, user.PasswordHash!))
                .Returns(false);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Auth.InvalidCredentials");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenUserIsNotActive()
        {
            // Arrange
            var command = new LoginUserCommand("test@example.com", "password123", "device123");

            // Create a user that is NOT active
            var user = User.RegisterNew("USR-250702-00001", command.Email, "hashed-password", "Test", "User", 1, null);
            // We do NOT call user.Activate()

            _mockUserRepository
                .Setup(r => r.GetByEmailAsync(command.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Auth.AccountNotActive");
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_WhenCredentialsAreValid()
        {
            // Arrange
            var command = new LoginUserCommand("test@example.com", "password123", "device123");
            var user = User.RegisterNew("USR-250702-00001", command.Email, "hashed-password", "Test", "User", 1, null);
            user.Activate(null);

            _mockUserRepository
                .Setup(r => r.GetByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _mockPasswordHasherService
                .Setup(p => p.VerifyPassword(command.Password, user.PasswordHash!))
                .Returns(true);

            // Simulate token generation
            var expectedJwt = "generated.jwt.token";
            var expectedExpiry = DateTime.UtcNow.AddHours(1);
            _mockJwtTokenGeneratorService
                .Setup(j => j.GenerateToken(user))
                .Returns((expectedJwt, expectedExpiry));

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Email.Should().Be(command.Email);
            result.Value.Token.Should().Be(expectedJwt);
            result.Value.RefreshToken.Should().NotBeNullOrEmpty();

            // Verify that a new refresh token was added via the repository
            _mockUserRepository.Verify(r => r.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), default), Times.Once);

            // Verify that changes were saved
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }
    }
}
