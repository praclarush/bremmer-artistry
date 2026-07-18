using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="Category"/>.
    /// </summary>
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="Category"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories", tb => tb.HasComment("Managed list of category options assignable to a piece, used to group it on the Gallery page."));

            builder.HasKey(c => c.Id).HasName("PK_Categories_Id");

            builder.HasAlternateKey(c => c.Name).HasName("UK_Categories_Name");

            builder.Property(c => c.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("Display name of the category, e.g. Bowls.");
        }
    }
}
