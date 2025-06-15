using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UMS.Domain.Authorization;

namespace UMS.Infrastructure.Persistence.Seeders
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(ApplicationDbContext dbContext, ILogger<DatabaseSeeder> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting database seeding...");

            await SeedPermissionsAsync(cancellationToken);
            await SeedRolesAsync(cancellationToken);
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
            const byte superAdminRoleId = 1;
            const string superAdminRoleName = "SuperAdmin";

            if(!await _dbContext.Roles.AnyAsync(r => r.Name == superAdminRoleName, cancellationToken))
            {
                _logger.LogInformation("Seeding '{RoleName}' role...", superAdminRoleName);
                var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);

                var superAdminRole = Role.Create(superAdminRoleId, superAdminRoleName);

                foreach(var permission in allPermissions)
                {
                    _dbContext.RolePermissions.Add(new RolePermission { RoleId = superAdminRole.Id, PermissionId = permission.Id });
                }

                await _dbContext.Roles.AddAsync(superAdminRole, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // --- User Role ---
            const byte userRoleId = 2;
            const string userRoleName = "User";

            if (!await _dbContext.Roles.AnyAsync(r => r.Name == userRoleName, cancellationToken))
            {
                _logger.LogInformation("Seeding '{RoleName}' role...", userRoleName);

                // Example: Give basic users only read access to their own user data
                // For now, we grant the general 'users:read' permission
                var userPermissions = await _dbContext.Permissions
                    .Where(p => p.Name == Permissions.Users.Read)
                    .ToListAsync(cancellationToken);

                var userRole = Role.Create(userRoleId, userRoleName);

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
