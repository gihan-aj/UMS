using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Extensions;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Reflection;
using UMS.Domain.Authorization;
using UMS.Domain.Clients;
using UMS.Domain.Users;
using UMS.Infrastructure.Persistence.Entities;

namespace UMS.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IPersistedGrantDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options) 
            : base(options)
        {
        }

        // --- Domain Aggregate Roots ---
        public DbSet<User> Users { get; set; } = null!; // null-forgiving operator, EF will initialize it

        public DbSet<Role> Roles { get; set; } = null!;

        public DbSet<Domain.Clients.Client> Clients { get; set; } = null!;

        // --- Other Domain Entities ---
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        public DbSet<Permission> Permissions { get; set; } = null!;

        public DbSet<Domain.Clients.ClientRedirectUri> ClientRedirectUris { get; set; } = null!;

        // --- Infrastructure & Sequence Entities ---
        public DbSet<ReferenceCodeSequence> ReferenceCodeSequences { get; set; } = null!;

        public DbSet<NumericSequence> NumericSequences { get; set; } = null!;

        // --- Join Tables ---
        public DbSet<UserRole> UserRoles { get; set; } = null!;

        public DbSet<RolePermission> RolePermissions { get; set; } = null!;

        // --- IdentityServer DbSets ---
        public DbSet<PersistedGrant> PersistedGrants { get; set; } = null!;

        public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; } = null!;

        public DbSet<Key> Keys { get; set; } = null!;

        public DbSet<ServerSideSession> ServerSideSessions { get; set; } = null!;

        public DbSet<PushedAuthorizationRequest> PushedAuthorizationRequests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // This method requires the OperationalStoreOptions, but it gets them
            // from the DI container internally, not from our constructor.
            modelBuilder.ConfigurePersistedGrantContext(new OperationalStoreOptions());

            // Apply all IEntityTypeConfiguration classes from the current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Apply snake_case naming convention for all tables and columns
            // This is a common way to do it, but ensure it covers all your needs (keys, indexes etc.)
            foreach(var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Set table name to snake_case
                entity.SetTableName(ToSnakeCase(entity.GetTableName() ?? entity.ClrType.Name));

                // Set column names to snake_case
                foreach(var property in entity.GetProperties())
                {
                    property.SetColumnName(ToSnakeCase(property.GetColumnName((StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema()))!) ?? property.Name));
                }

                // Set key names to snake_case
                foreach (var key in entity.GetKeys())
                {
                    key.SetName(ToSnakeCase(key.GetName() ?? ""));
                }

                // Set foreign key names to snake_case
                foreach (var fk in entity.GetForeignKeys())
                {
                    fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName() ?? ""));
                }

                // Set index names to snake_case
                foreach (var index in entity.GetIndexes())
                {
                    index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName() ?? ""));
                }
            }
        }

        private static string ToSnakeCase(string input)
        {
            if(string.IsNullOrEmpty(input)) return input;

            // A more robust snake_case conversion might be needed for various PascalCase/camelCase inputs.
            // This is a simplified version.
            // Example: "UserRole" -> "user_role", "EmailAddress" -> "email_address"
            // Handles sequences of uppercase letters like "URL" -> "u_r_l" (might not be desired, adjust if needed)
            return string.Concat(input.Select((x, i) =>
                i > 0 && char.IsUpper(x) && (char.IsLower(input[i - 1]) || (i < input.Length - 1 && char.IsLower(input[i + 1])) || (char.IsUpper(input[i - 1]) && i < input.Length - 1 && char.IsLower(input[i + 1])))
                ? "_" + x.ToString()
                : x.ToString()
            )).ToLowerInvariant();
        }
    }
}
