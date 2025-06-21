using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UMS.Domain.Authorization;

namespace UMS.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(r => r.Id);

            // Tell EF Core that the application will provide the value for the primary key,
            // and the database should not try to generate it.
            builder.Property(r => r.Id).ValueGeneratedNever();

            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(r => r.Name)
                .IsUnique();

            builder.Property(r => r.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(r => r.DeletedAtUtc)
                .IsRequired(false);

            builder.Property(r => r.DeletedBy)
                .IsRequired(false);

            // Queries for Roles automatically filter out soft-deleted records
            builder.HasQueryFilter(r => !r.IsDeleted);

            // Ignore the Permission collection for direct mapping; it's linked via the join entity.
            // EF Core will handle this relationship through the RolePermission configuration.
            // builder.Ignore(r => r.Permissions);

            builder.HasMany(r => r.Permissions)
                .WithOne()
                .HasForeignKey(rp => rp.RoleId)
                .IsRequired();
        }
    }
}
