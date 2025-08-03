using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Features.Roles.Commands.CreateRole;
using UMS.Domain.Authorization;

namespace UMS.Application.UnitTests.Features.Roles.Commands
{
    public class CreateRoleCommandHandlerTests
    {
        // Mock the handlers dependencies
        private readonly Mock<IRoleRepository> _mockRoleRepository;
        private readonly Mock<IPermissionRepository> _mockPermissionRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ISequenceGeneratorService> _mockSequenceGeneratorService;
        private readonly Mock<ICurrentUserService> _mockUserService;

        // The handler instance we are testing
        private readonly CreateRoleCommandHandler _handler;

        public CreateRoleCommandHandlerTests()
        {
            // --- Arrange (Setup) ---
            // The constructor runs before each test, setting up a clean environment

            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockSequenceGeneratorService = new Mock<ISequenceGeneratorService>();
            _mockUserService = new Mock<ICurrentUserService>();
            _mockPermissionRepository = new Mock<IPermissionRepository>();

            // We can use a mock for ILogger, as we don't need to check the log output in these tests.
            var mockLogger = new Mock<ILogger<CreateRoleCommandHandler>>();

            // Create an instance of the handler, injecting the mocked dependencies.
            // We use .Object to get the actual mock object instance.
            _handler = new CreateRoleCommandHandler(
                _mockRoleRepository.Object,
                _mockUnitOfWork.Object,
                mockLogger.Object,
                _mockSequenceGeneratorService.Object,
                _mockUserService.Object,
                _mockPermissionRepository.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailureResult_WhenRoleNameAlreadyExists()
        {
            // --- Arrange ---
            // 1. Define the input for the test
            var command = new CreateRoleCommand("ExistingRole", new List<string>());

            // 2. Set up the mock repository to simulate finding an existing role.
            // When GetByNameAsync is called with "ExistingRole", it should return a Role object
            _mockRoleRepository
                .Setup(r => r.GetByNameAsync(command.Name, default))
                .ReturnsAsync(Role.Create(1, "ExixstingRole", Guid.Empty));

            // --- Act ---
            // 3. Execute the handler with the command
            var result = await _handler.Handle(command, default);

            // --- Assert ---
            // 4. Verify the outcome
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Role.AlreadyExists");
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccessResult_WhenRoleIsCreated()
        {
            // --- Arrange ---
            var permissionNames = new List<string> { "users:read", "roles:create" };
            var command = new CreateRoleCommand("NewRole", permissionNames);
            byte newRoleId = 3;

            // 1. Simulate that no role with this name exists.
            _mockRoleRepository
                .Setup(r => r.GetByNameAsync(command.Name, default))
                .ReturnsAsync((Role?)null);

            // 2. Simulate the sequence generator returning the next ID.
            _mockSequenceGeneratorService
                .Setup(s => s.GetNextIdAsync<byte>("Roles", default))
                .ReturnsAsync(newRoleId);

            // 3. Simulate the PermissionRepository returning the requested permissions.
            var permissionsFromDb = new List<Permission>
            {
                Permission.Create(1, "users:read"),
                Permission.Create(5, "roles:create")
            };
            _mockPermissionRepository
                .Setup(p => p.GetPermissionsByNameRangeAsync(permissionNames, default))
                .ReturnsAsync(permissionsFromDb);

            // --- Act ---
            var result = await _handler.Handle(command, default);

            // --- Assert ---
            // 1. Check that the operation was successful and returned the correct new ID.
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(newRoleId);

            // 2. Verify that the repository's AddAsync method was called.
            _mockRoleRepository.Verify(r => r.AddAsync(It.IsAny<Role>(), default), Times.Once());

            // 3. Verify that we added the correct permissions to the join table via the repository.
            _mockRoleRepository.Verify(
                r => r.AddRolePermissionsRangeAsync(
                    It.Is<List<RolePermission>>(list =>
                        list.Count == 2 &&
                        list.Any(rp => rp.RoleId == newRoleId && rp.PermissionId == 1) &&
                        list.Any(rp => rp.RoleId == newRoleId && rp.PermissionId == 5)),
                    default),
                Times.Once);

            // 4. Verify that SaveChangesAsync was called.
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once());
        }
    }
}
