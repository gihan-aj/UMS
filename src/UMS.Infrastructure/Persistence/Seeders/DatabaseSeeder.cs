using System;
using System.Collections.Generic;
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
            _logger.LogInformation("Starting database seeding process...");

            await SeedPermissionsAsync(cancellationToken);
            await SeedRolesAsync(cancellationToken);
            await SeedSuperAdminAsync(cancellationToken);

            _logger.LogInformation("Database seeding preparation complete. Changes will be saved by the Unit of Work.");
        }

        private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
        {
            var definedPermissions = Permissions.GetAllPermissionValues();
            var existingPermissionNames = await _dbContext.Permissions
                .Select(p => p.Name)
                .ToListAsync(cancellationToken);

            var newPermissionNames = definedPermissions.Except(existingPermissionNames).ToList();

            if (newPermissionNames.Any())
            {
                _logger.LogInformation("Seeding {Count} new permissions...", newPermissionNames.Count);

                foreach (var name in newPermissionNames)
                {
                    short newPermissionId = await _sequenceGenerator.GetNextIdAsync<short>("Permissions", cancellationToken);
                    var newPermission = Permission.Create(newPermissionId, name);
                    await _dbContext.Permissions.AddAsync(newPermission);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

            }
            else
            {
                _logger.LogInformation("Permissions are already up-to-date.");
            }
        }

        private async Task SeedRolesAsync(CancellationToken cancellationToken)
        {
            // --- Seed SuperAdmin Role ---
            var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);
            await SeedRoleAsync("SuperAdmin", allPermissions, cancellationToken);

            // --- Seed User Role ---
            var selfServicePermissionNames = new List<string>
            {
                Permissions.Profile.Read,
                Permissions.Profile.Update,
                Permissions.Profile.ChangePassword,
                Permissions.Profile.ChangeEmail,
            };
            var userPermissions = allPermissions.Where(p => selfServicePermissionNames.Contains(p.Name)).ToList();
            await SeedRoleAsync("User", userPermissions, cancellationToken);
        }

        private async Task SeedRoleAsync(string roleName, List<Permission> desiredPermissions, CancellationToken cancellationToken)
        {
            var existingRole = await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

            if (existingRole == null)
            {
                _logger.LogInformation("Creating '{RoleName}' role...", roleName);
                byte newRoleId = await _sequenceGenerator.GetNextIdAsync<byte>("Roles", cancellationToken);
                var newRole = Role.Create(newRoleId, roleName, null, Guid.Empty); // System created

                foreach (var permission in desiredPermissions)
                {
                    _dbContext.RolePermissions.Add(new RolePermission { RoleId = newRole.Id, PermissionId = permission.Id });
                }
                await _dbContext.Roles.AddAsync(newRole, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // Role exists, so sync its permissions
                var currentPermissionIds = existingRole.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
                var desiredPermissionIds = desiredPermissions.Select(p => p.Id).ToHashSet();

                var permissionsToAdd = desiredPermissions.Where(p => !currentPermissionIds.Contains(p.Id)).ToList();
                var permissionsToRemove = existingRole.RolePermissions.Where(rp => !desiredPermissionIds.Contains(rp.PermissionId)).ToList();

                if (permissionsToAdd.Any())
                {
                    _logger.LogInformation("Adding {Count} new permissions to '{RoleName}' role.", permissionsToAdd.Count, roleName);
                    foreach (var permission in permissionsToAdd)
                    {
                        _dbContext.RolePermissions.Add(new RolePermission { RoleId = existingRole.Id, PermissionId = permission.Id });
                    }  
                }

                if (permissionsToRemove.Any())
                {
                    _logger.LogInformation("Removing {Count} obsolete permissions from '{RoleName}' role.", permissionsToRemove.Count, roleName);
                    _dbContext.RolePermissions.RemoveRange(permissionsToRemove);
                }

                if (!permissionsToAdd.Any() && !permissionsToRemove.Any())
                {
                    _logger.LogInformation("'{RoleName}' role permissions are already up-to-date.", roleName);
                }
                else
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
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
                    _logger.LogError("'SuperAdmin' role not found. Run role seeding first. Cannot seed SuperAdmin user.");
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
                adminUser.Activate(null); // Activate the admin account immediately

                await _dbContext.Users.AddAsync(adminUser, cancellationToken);
            }
        }
    }
}
