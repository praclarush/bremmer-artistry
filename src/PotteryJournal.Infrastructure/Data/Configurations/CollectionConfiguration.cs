using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="Collection"/>.
    /// </summary>
    public class CollectionConfiguration : IEntityTypeConfiguration<Collection>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="Collection"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<Collection> builder)
        {
            builder.ToTable("Collections", tb => tb.HasComment("A named grouping of pieces, independent of Category. At most one is featured on the homepage."));

            builder.HasKey(c => c.Id).HasName("PK_Collections_Id");

            builder.HasAlternateKey(c => c.Name).HasName("UK_Collections_Name");

            builder.Property(c => c.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("Display name of the collection, e.g. Lightning-Cracked Collection.");

            builder.Property(c => c.IsFeaturedOnHomepage)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Whether this collection's pieces are shown in the homepage's rotating display. At most one collection is featured at a time.");
        }
    }
}
