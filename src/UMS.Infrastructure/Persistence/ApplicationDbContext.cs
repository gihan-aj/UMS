using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq;
using System.Reflection;
using UMS.Domain.Users;

namespace UMS.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options)
        {
        }

        // DbSet for each Aggregate Root
        public DbSet<User> Users { get; set; } = null!; // null-forgiving operator, EF will initialize it

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
