using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="PieceImage"/>.
    /// </summary>
    public class PieceImageConfiguration : IEntityTypeConfiguration<PieceImage>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="PieceImage"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<PieceImage> builder)
        {
            builder.ToTable("PieceImages", tb => tb.HasComment("An uploaded photo belonging to a pottery piece."));

            builder.HasKey(i => i.Id).HasName("PK_PieceImages_Id");

            builder.HasIndex(i => i.PieceId).HasDatabaseName("IX_PieceImages_PieceId");

            builder.Property(i => i.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(i => i.PieceId)
                .IsRequired()
                .HasComment("The piece this photo belongs to.");

            builder.Property(i => i.FileName)
                .IsRequired()
                .HasMaxLength(260)
                .HasComment("File name of the resized, re-encoded photo on the uploads volume.");

            builder.Property(i => i.SortOrder)
                .IsRequired()
                .HasComment("Display order of this photo among a piece's photos.");

            builder.Property(i => i.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("now()")
                .HasComment("Timestamp the photo was uploaded.");
        }
    }
}
