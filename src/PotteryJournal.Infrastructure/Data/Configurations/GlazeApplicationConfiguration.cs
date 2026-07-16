using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="GlazeApplication"/>.
    /// </summary>
    public class GlazeApplicationConfiguration : IEntityTypeConfiguration<GlazeApplication>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="GlazeApplication"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<GlazeApplication> builder)
        {
            builder.ToTable("GlazeApplications", tb => tb.HasComment("A glaze application recorded against a pottery piece."));

            builder.HasKey(g => g.Id).HasName("PK_GlazeApplications_Id");

            builder.HasIndex(g => g.PieceId).HasDatabaseName("IX_GlazeApplications_PieceId");

            builder.Property(g => g.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(g => g.PieceId)
                .IsRequired()
                .HasComment("The piece this glaze application belongs to.");

            builder.Property(g => g.Location)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("Where on the piece the glaze was applied, e.g. Interior.");

            builder.Property(g => g.GlazeName)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("Name of the glaze used.");

            builder.Property(g => g.Coats)
                .IsRequired()
                .HasComment("Number of coats applied.");
        }
    }
}
