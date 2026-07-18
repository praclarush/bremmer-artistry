using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="ClayBody"/>.
    /// </summary>
    public class ClayBodyConfiguration : IEntityTypeConfiguration<ClayBody>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="ClayBody"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<ClayBody> builder)
        {
            builder.ToTable("ClayBodies", tb => tb.HasComment("Managed list of clay body options assignable to a piece."));

            builder.HasKey(c => c.Id).HasName("PK_ClayBodies_Id");

            builder.HasAlternateKey(c => c.Name).HasName("UK_ClayBodies_Name");

            builder.Property(c => c.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("Display name of the clay body, e.g. Reclaim.");
        }
    }
}
