using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UMS.Domain.Clients;

namespace UMS.Infrastructure.Persistence.Configurations
{
    public class ClientRedirectUriConfiguration : IEntityTypeConfiguration<ClientRedirectUri>
    {
        public void Configure(EntityTypeBuilder<ClientRedirectUri> builder)
        {
            builder.HasKey(ru => ru.Id);

            builder.Property(ru => ru.Uri)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasIndex(ru => new { ru.ClientId, ru.Uri })
                .IsUnique();
        }
    }
}
