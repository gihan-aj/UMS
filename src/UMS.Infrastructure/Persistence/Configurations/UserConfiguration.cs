using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UMS.Domain.Users;

namespace UMS.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Table name will be set to "users" by the snake_case convention in DbContext.
            // If you want to override it specifically: builder.ToTable("custom_user_table_name");

            // Primary Key (Id property from base Entity<Guid>)
            // Column name will be "id" by convention + snake_case.
            builder.HasKey(u => u.Id);

            // --- Properties ---

            // UserCode
            builder.Property(u => u.UserCode)
                .IsRequired()
                .HasMaxLength(20); // Adjust length as needed (e.g., USR-YYMMDD-NNNNN is ~16 chars)
            builder.HasIndex(u => u.UserCode)
                .IsUnique();

            // Email
            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);
            builder.HasIndex(u => u.Email)
                .IsUnique();

            // FirstName
            builder.Property(u => u.FirstName)
                .HasMaxLength(100); // Optional, so IsRequired(false) is default

            // LastName
            builder.Property(u => u.LastName)
                .HasMaxLength(100); // Optional

            // PasswordHash
            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255); // Adjust based on your hashing algorithm output length

            // IsActive
            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(false);

            // LastLoginAtUtc
            builder.Property(u => u.LastLoginAtUtc)
                .IsRequired(false); // Nullable

            // --- AuditableEntity Properties (from base class) ---
            // These will be automatically mapped. Column names (created_at_utc, created_by, etc.)
            // will be handled by the snake_case convention.
            // builder.Property(u => u.CreatedAtUtc).IsRequired();
            // builder.Property(u => u.CreatedBy).IsRequired(false); // Nullable Guid
            // builder.Property(u => u.LastModifiedAtUtc).IsRequired(false);
            // builder.Property(u => u.LastModifiedBy).IsRequired(false);

            // --- ISoftDeletable Properties ---
            builder.Property(u => u.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.DeletedAtUtc)
                .IsRequired(false); // Nullable

            builder.Property(u => u.DeletedBy)
                .IsRequired(false); // Nullable Guid

            // --- Configure Global Query Filter for Soft Delete ---
            // This ensures that queries for Users automatically filter out soft-deleted records.
            builder.HasQueryFilter(u => !u.IsDeleted);

            // --- Relationships (if any defined directly on User) ---
            // e.g., builder.HasMany(u => u.Roles)...

            // --- Ignoring DomainEvents ---
            // The DomainEvents collection in AggregateRoot is not meant to be persisted.
            //builder.Ignore(u => u.GetDomainEvents());

        }
    }
}
