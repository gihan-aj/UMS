using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UMS.Domain.Users;

namespace UMS.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(256);
            builder.HasIndex(rt => rt.Token)
                .IsUnique();

            builder.Property(rt => rt.DeviceId)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(rt => rt.ExpiresAtUtc)
                .IsRequired();
            builder.Property(rt => rt.CreatedAtUtc)
                .IsRequired();
            builder.Property(rt => rt.RevokedAtUtc);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
