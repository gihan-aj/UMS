using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UMS.Domain.Authorization;

namespace UMS.Infrastructure.Persistence.Configurations
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            // Composite primary key
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // Define the relationship to Permission to enable .ThenInclude
            builder.HasOne(rp => rp.Permission)
                .WithMany()
                .HasForeignKey(rp => rp.PermissionId)
                .IsRequired();
        }
    }
}
