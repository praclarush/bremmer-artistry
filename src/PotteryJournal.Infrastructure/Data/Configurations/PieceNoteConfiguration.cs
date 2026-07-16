using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="PieceNote"/>.
    /// </summary>
    public class PieceNoteConfiguration : IEntityTypeConfiguration<PieceNote>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="PieceNote"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<PieceNote> builder)
        {
            builder.ToTable("PieceNotes", tb => tb.HasComment("A note recorded against a pottery piece."));

            builder.HasKey(n => n.Id).HasName("PK_PieceNotes_Id");

            builder.HasIndex(n => n.PieceId).HasDatabaseName("IX_PieceNotes_PieceId");

            builder.Property(n => n.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(n => n.PieceId)
                .IsRequired()
                .HasComment("The piece this note belongs to.");

            builder.Property(n => n.Title)
                .HasMaxLength(200)
                .HasComment("Optional note heading, e.g. a technique name.");

            builder.Property(n => n.NoteText)
                .IsRequired()
                .HasComment("The note body text.");

            builder.Property(n => n.SortOrder)
                .IsRequired()
                .HasComment("Display order of this note among a piece's notes.");
        }
    }
}
