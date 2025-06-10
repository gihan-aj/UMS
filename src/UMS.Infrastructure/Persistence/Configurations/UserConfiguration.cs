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
            builder.HasKey(u => u.Id);

            // --- Properties ---

            builder.Property(u => u.UserCode)
                .IsRequired()
                .HasMaxLength(50);
            builder.HasIndex(u => u.UserCode)
                .IsUnique();

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);
            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.FirstName)
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .HasMaxLength(100);

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            // ActivationToken - Set max length
            builder.Property(u => u.ActivationToken)
                .HasMaxLength(256);

            // PasswordResetToken - Set max length
            builder.Property(u => u.PasswordResetToken)
                .HasMaxLength(256);

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.LastLoginAtUtc)
                .IsRequired(false);

            // --- ISoftDeletable Properties ---
            builder.Property(u => u.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.DeletedAtUtc)
                .IsRequired(false);

            builder.Property(u => u.DeletedBy)
                .IsRequired(false);

            // --- Configure Global Query Filter for Soft Delete ---
            builder.HasQueryFilter(u => !u.IsDeleted);

            // --- Ignoring DomainEvents ---
            builder.Ignore(u => u.GetDomainEvents());

        }
    }
}
