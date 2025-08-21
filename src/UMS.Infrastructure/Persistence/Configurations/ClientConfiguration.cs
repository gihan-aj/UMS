using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UMS.Domain.Clients;

namespace UMS.Infrastructure.Persistence.Configurations
{
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.ClientId)
                .IsRequired()
                .HasMaxLength(40);
            builder.HasIndex(c => c.ClientId)
                .IsUnique();

            builder.Property(c => c.ClientName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.ClientSecretHash)
                .IsRequired()
                .HasMaxLength(256);

            // Configure the one-to-many relationship with RedirectUris
            builder.HasMany(c => c.RedirectUris)
                .WithOne() // No navigation property back to Client from RedirectUri
                .HasForeignKey(ru => ru.ClientId)
                .OnDelete(DeleteBehavior.Cascade); // If a client is deleted, its URIs are deleted too

            builder.Property(r => r.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(r => r.DeletedAtUtc)
                .IsRequired(false);

            builder.Property(r => r.DeletedBy)
                .IsRequired(false);

            // Queries for Roles automatically filter out soft-deleted records
            builder.HasQueryFilter(r => !r.IsDeleted);
        }
    }
}
