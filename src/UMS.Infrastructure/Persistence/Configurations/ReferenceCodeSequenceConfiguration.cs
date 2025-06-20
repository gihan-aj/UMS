using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UMS.Infrastructure.Persistence.Entities;

namespace UMS.Infrastructure.Persistence.Configurations
{
    public class ReferenceCodeSequenceConfiguration : IEntityTypeConfiguration<ReferenceCodeSequence>
    {
        public void Configure(EntityTypeBuilder<ReferenceCodeSequence> builder)
        {
            // Table name will be "entity_sequences" by convention + snake_case in DbContext.
            // If you want to override: builder.ToTable("your_custom_sequence_table_name");

            //Composite primary key
            builder.HasKey(es => new { es.EntityTypePrefix, es.SequenceDate });

            builder.Property(es => es.EntityTypePrefix)
                .IsRequired()
                .HasMaxLength(4);

            builder.Property(es => es.SequenceDate)
                .IsRequired()
                .HasColumnType("date"); // Ensure it is stored as a date without time

            builder.Property(es => es.LastValue)
                .IsRequired()
                .HasDefaultValue(0);
        }
    }
}
