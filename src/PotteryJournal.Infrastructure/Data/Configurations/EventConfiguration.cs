using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="Event"/>.
    /// </summary>
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="Event"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("Events", tb => tb.HasComment("A public event shown on the Events page and calendar."));

            builder.HasKey(e => e.Id).HasName("PK_Events_Id");

            builder.HasIndex(e => e.StartDateTime).HasDatabaseName("IX_Events_StartDateTime");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200)
                .HasComment("Event title.");

            builder.Property(e => e.Description)
                .IsRequired()
                .HasComment("Event description shown on the card and detail view.");

            builder.Property(e => e.StartDateTime)
                .IsRequired()
                .HasComment("Event start date and time.");

            builder.Property(e => e.EndDateTime)
                .HasComment("Event end date and time, if known.");

            builder.Property(e => e.VenueName)
                .HasMaxLength(200)
                .HasComment("Venue name shown on the event card.");

            builder.Property(e => e.VenueAddress)
                .HasMaxLength(300)
                .HasComment("Venue address, used for the card's map link.");

            builder.Property(e => e.ImageFileName)
                .HasMaxLength(260)
                .HasComment("File name of the event's banner photo on the uploads volume.");

            builder.Property(e => e.ExternalLinkUrl)
                .HasMaxLength(500)
                .HasComment("Optional external link, e.g. a ticketing or host page.");

            builder.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("now()")
                .HasComment("Timestamp the event was created in the admin area.");

            builder.Property(e => e.CreatedByEmail)
                .IsRequired()
                .HasMaxLength(320)
                .HasComment("Email of the admin who created the event.");
        }
    }
}
