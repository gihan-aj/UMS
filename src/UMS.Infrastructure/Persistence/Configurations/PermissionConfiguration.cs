using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UMS.Domain.Authorization;

namespace UMS.Infrastructure.Persistence.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.HasKey(p => p.Id);

            // Tell EF Core that the application will provide the value for the primary key,
            // and the database should not try to generate it.
            //builder.Property(p => p.Id).ValueGeneratedNever();

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(128);

            // A permission name must be unique, either globally (if ClientId is null)
            // or within the scope of a client.
            builder.HasIndex(p => new { p.ClientId, p.Name })
                .IsUnique();

            // --- Configure the Nullable Foreign Key Relationship ---
            // This defines the relationship to the Client entity.
            // Because the foreign key property (ClientId) is nullable (Guid?),
            // EF Core understands this is an optional relationship.
            // We don't need to specify .IsRequired(false) as it's inferred from the nullable type.
            builder.HasOne(p => p.Client)
                .WithMany() // Assuming Client doesn't need a direct navigation back to its Permissions
                .HasForeignKey(p => p.ClientId);
        }
    }
}
