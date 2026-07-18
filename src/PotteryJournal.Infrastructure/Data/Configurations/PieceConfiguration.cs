using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="Piece"/>.
    /// </summary>
    public class PieceConfiguration : IEntityTypeConfiguration<Piece>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="Piece"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<Piece> builder)
        {
            builder.ToTable("Pieces", tb => tb.HasComment("A pottery piece entry in the Pottery Journal catalog."));

            builder.HasKey(p => p.Id).HasName("PK_Pieces_Id");

            builder.HasAlternateKey(p => p.PieceNumber).HasName("UK_Pieces_PieceNumber");

            builder.Property(p => p.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(p => p.PieceNumber)
                .IsRequired()
                .HasComment("Sequential, human-facing project number shown as e.g. #003.");

            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(200)
                .HasComment("Display title of the piece.");

            builder.Property(p => p.CategoryId)
                .HasComment("Managed category assigned to this piece, used to group it on the Gallery page when ShowInGallery is set.");

            builder.Property(p => p.ShowInGallery)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Whether this piece is curated for public display on the Gallery page. The Gallery is independent of the Pottery Journal -- a piece can be logged for record-keeping without ever appearing here.");

            builder.Property(p => p.ClayBodyId)
                .HasComment("Managed clay body assigned to this piece, if known.");

            builder.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .HasConstraintName("FK_Pieces_Categories_CategoryId")
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(p => p.ClayBody)
                .WithMany()
                .HasForeignKey(p => p.ClayBodyId)
                .HasConstraintName("FK_Pieces_ClayBodies_ClayBodyId")
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(p => p.CollectionId)
                .HasComment("Managed collection this piece belongs to, if any. Independent of Category.");

            builder.HasOne(p => p.Collection)
                .WithMany()
                .HasForeignKey(p => p.CollectionId)
                .HasConstraintName("FK_Pieces_Collections_CollectionId")
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(p => p.StartedDate)
                .IsRequired()
                .HasComment("Date work on the piece began.");

            builder.Property(p => p.FinishedDate)
                .HasComment("Date the piece was finished, if complete.");

            builder.Property(p => p.SizeText)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("Free-text dimensions, e.g. 6\" x 4\".");

            builder.Property(p => p.WeightText)
                .IsRequired()
                .HasMaxLength(50)
                .HasComment("Free-text weight of the piece.");

            builder.Property(p => p.GlazeSummary)
                .IsRequired()
                .HasMaxLength(500)
                .HasComment("Short summary of the glaze treatment shown on the worksheet.");

            builder.Property(p => p.AttachmentsText)
                .HasMaxLength(500)
                .HasComment("Free-text note of any physical attachments recorded for the piece.");

            builder.Property(p => p.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("now()")
                .HasComment("Timestamp the entry was created in the admin area.");

            builder.Property(p => p.CreatedByEmail)
                .IsRequired()
                .HasMaxLength(320)
                .HasComment("Email of the admin who created the entry.");

            builder.HasMany(p => p.Notes)
                .WithOne(n => n.Piece)
                .HasForeignKey(n => n.PieceId)
                .HasConstraintName("FK_PieceNotes_Pieces_PieceId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Images)
                .WithOne(i => i.Piece)
                .HasForeignKey(i => i.PieceId)
                .HasConstraintName("FK_PieceImages_Pieces_PieceId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.GlazeApplications)
                .WithOne(g => g.Piece)
                .HasForeignKey(g => g.PieceId)
                .HasConstraintName("FK_GlazeApplications_Pieces_PieceId")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
