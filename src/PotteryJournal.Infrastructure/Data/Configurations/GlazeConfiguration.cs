using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="Glaze"/>.
    /// </summary>
    public class GlazeConfiguration : IEntityTypeConfiguration<Glaze>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="Glaze"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<Glaze> builder)
        {
            builder.ToTable("Glazes", tb => tb.HasComment("Managed list of glaze options assignable to a glaze application."));

            builder.HasKey(g => g.Id).HasName("PK_Glazes_Id");

            builder.HasAlternateKey(g => g.Name).HasName("UK_Glazes_Name");

            builder.Property(g => g.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(g => g.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("Display name of the glaze, e.g. Waterfall.");
        }
    }
}
