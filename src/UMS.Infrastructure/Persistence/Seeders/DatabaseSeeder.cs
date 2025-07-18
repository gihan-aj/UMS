using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UMS.Application.Abstractions.Services;
using UMS.Application.Settings;
using UMS.Domain.Authorization;
using UMS.Domain.Users;
using UMS.Infrastructure.Services;
using UMS.Infrastructure.Settings;

namespace UMS.Infrastructure.Persistence.Seeders
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DatabaseSeeder> _logger;
        IPasswordHasherService _passwordHasher;
        IReferenceCodeGeneratorService _codeGenerator;
        ISequenceGeneratorService _sequenceGenerator;
        AdminSettings _adminSettings;
        TokenSettings _tokenSettings;

        public DatabaseSeeder(
            ApplicationDbContext dbContext,
            ILogger<DatabaseSeeder> logger,
            IPasswordHasherService passwordHasher,
            IReferenceCodeGeneratorService codeGenerator,
            IOptions<AdminSettings> adminSettings,
            IOptions<TokenSettings> tokenSettings,
            ISequenceGeneratorService sequenceGenerator)
        {
            _dbContext = dbContext;
            _logger = logger;
            _passwordHasher = passwordHasher;
            _codeGenerator = codeGenerator;
            _adminSettings = adminSettings.Value;
            _sequenceGenerator = sequenceGenerator;
            _tokenSettings = tokenSettings.Value;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting database seeding...");

            await SeedPermissionsAsync(cancellationToken);
            await SeedRolesAsync(cancellationToken);
            await SeedSuperAdminAsync(cancellationToken);
        }

        private async Task SeedSuperAdminAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_adminSettings.Email) || string.IsNullOrEmpty(_adminSettings.Password))
            {
                _logger.LogWarning("Admin user credentials are not configured. Skipping SuperAdmin seeding.");
                return;
            }

            if (!await _dbContext.Users.AnyAsync(u => u.Email == _adminSettings.Email, cancellationToken))
            {
                _logger.LogInformation("Seeding 'SuperAdmin' user account...");

                var superAdminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin", cancellationToken);
                if (superAdminRole == null)
                {
                    _logger.LogError("'SuperAdmin' role not found. Cannot seed SuperAdmin user.");
                    return;
                }

                string userCode = await _codeGenerator.GenerateReferenceCodeAsync("USR");
                string passwordHash = _passwordHasher.HashPassword(_adminSettings.Password);

                var adminUser = User.RegisterNew(
                    userCode,
                    _adminSettings.Email,
                    passwordHash,
                    _adminSettings.FirstName,
                    _adminSettings.LastName,
                    _tokenSettings.ActivationTokenExpiryHours,
                    Guid.Empty); // System created

                adminUser.AssignRole(superAdminRole.Id, Guid.Empty);
                adminUser.Activate(Guid.Empty); // Activate the admin account immediately

                await _dbContext.Users.AddAsync(adminUser, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task SeedPermissionsAsync(CancellationToken cancellationToken = default)
        {
            var definedPermissions = Permissions.GetAllPermissionValues();
            var existingPermissions = await _dbContext.Permissions
                .Select(p => p.Name)
                .ToListAsync(cancellationToken);

            var newPermissionNames = definedPermissions
                .Except(existingPermissions)
                .ToList();

            if(newPermissionNames.Any())
            {
                _logger.LogInformation("Seeding {Count} new permissions...", newPermissionNames.Count);

                // Start indexing from next available ID
                short lastId = await _dbContext.Permissions.AnyAsync(cancellationToken)
                    ? (await _dbContext.Permissions.MaxAsync(p => p.Id, cancellationToken))
                    : (short)0;

                var newPermissions = newPermissionNames
                    .Select(name => Permission.Create(++lastId, name))
                    .ToList();

                await _dbContext.Permissions.AddRangeAsync(newPermissions, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _logger.LogInformation("Permissions are already up-to-date.");
            }
        }

        private async Task SeedRolesAsync(CancellationToken cancellationToken = default)
        {
            // --- SuperAdmin Role ---
            const string superAdminRoleName = "SuperAdmin";

            if(!await _dbContext.Roles.AnyAsync(r => r.Name == superAdminRoleName, cancellationToken))
            {
                _logger.LogInformation("Seeding '{RoleName}' role...", superAdminRoleName);
                var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);

                byte superAdminRoleId = await _sequenceGenerator.GetNextIdAsync<byte>("Roles", cancellationToken);
                var superAdminRole = Role.Create(superAdminRoleId, superAdminRoleName, Guid.Empty);

                foreach(var permission in allPermissions)
                {
                    _dbContext.RolePermissions.Add(new RolePermission { RoleId = superAdminRole.Id, PermissionId = permission.Id });
                }

                await _dbContext.Roles.AddAsync(superAdminRole, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // --- User Role ---
            const string userRoleName = "User";

            if (!await _dbContext.Roles.AnyAsync(r => r.Name == userRoleName, cancellationToken))
            {
                _logger.LogInformation("Seeding '{RoleName}' role...", userRoleName);

                // Example: Give basic users only read access to their own user data
                // For now, we grant the general 'users:read' permission
                var userPermissions = await _dbContext.Permissions
                    .Where(p => p.Name == Permissions.Users.Read)
                    .ToListAsync(cancellationToken);

                byte userRoleId = await _sequenceGenerator.GetNextIdAsync<byte>("Roles", cancellationToken);
                var userRole = Role.Create(userRoleId, userRoleName, Guid.Empty);

                foreach(var permission in userPermissions)
                {
                    _dbContext.RolePermissions.Add(new RolePermission { RoleId = userRole.Id, PermissionId = permission.Id });
                }

                await _dbContext.Roles.AddAsync(userRole, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
