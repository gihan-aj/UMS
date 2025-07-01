using Microsoft.Extensions.Logging;
using Moq;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Features.Roles.Commands.CreateRole;

namespace UMS.Application.UnitTests.Features.Roles.Commands
{
    public class CreateRoleCommandHandlerTests
    {
        // Mock the handlers dependencies
        private readonly Mock<IRoleRepository> _mockRoleRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ISequenceGeneratorService> _sequenceGeneratorService;
        private readonly Mock<ICurrentUserService> _mockUserService;

        // The handler instance we are testing
        private readonly CreateRoleCommandHandler _handler;

        public CreateRoleCommandHandlerTests()
        {
            // --- Arrange (Setup) ---
            // The constructor runs before each test, setting up a clean environment

            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _sequenceGeneratorService = new Mock<ISequenceGeneratorService>();
            _mockUserService = new Mock<ICurrentUserService>();

            // We can use a mock for ILogger, as we don't need to check the log output in these tests.
            var mockLogger = new Mock<ILogger<CreateRoleCommandHandler>>();

            // Create an instance of the handler, injecting the mocked dependencies.
            // We use .Object to get the actual mock object instance.
            _handler = new CreateRoleCommandHandler(
                _mockRoleRepository.Object,
                _mockUnitOfWork.Object,
                mockLogger.Object,
                _sequenceGeneratorService.Object,
                _mockUserService.Object);
        }
    }
}
