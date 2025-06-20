using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UMS.Infrastructure.Persistence.Entities;

namespace UMS.Infrastructure.Persistence.Configurations
{
    public class NumericSequenceConfiguration : IEntityTypeConfiguration<NumericSequence>
    {
        public void Configure(EntityTypeBuilder<NumericSequence> builder)
        {
            builder.HasKey(s => s.SequenceName);

            builder.Property(s => s.SequenceName)
                .HasMaxLength(100);

            builder.Property(s => s.LastValue)
                .IsRequired()
                .HasDefaultValue(0L);
        }
    }
}
