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
            builder.Property(p => p.Id).ValueGeneratedNever();

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(128);

            builder.HasIndex(p => p.Name)
                .IsUnique();
        }
    }
}
