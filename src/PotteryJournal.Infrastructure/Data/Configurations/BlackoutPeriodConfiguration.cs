using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="BlackoutPeriod"/>.
    /// </summary>
    public class BlackoutPeriodConfiguration : IEntityTypeConfiguration<BlackoutPeriod>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="BlackoutPeriod"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<BlackoutPeriod> builder)
        {
            builder.ToTable("BlackoutPeriods", tb => tb.HasComment("An admin-managed date/time range during which class bookings can't be made."));

            builder.HasKey(b => b.Id).HasName("PK_BlackoutPeriods_Id");

            builder.HasIndex(b => b.StartDateTime).HasDatabaseName("IX_BlackoutPeriods_StartDateTime");

            builder.Property(b => b.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(b => b.StartDateTime)
                .IsRequired()
                .HasComment("Start of the blacked-out range.");

            builder.Property(b => b.EndDateTime)
                .IsRequired()
                .HasComment("End of the blacked-out range.");

            builder.Property(b => b.Reason)
                .HasMaxLength(300)
                .HasComment("Optional note explaining why this range is blacked out.");

            builder.Property(b => b.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("now()")
                .HasComment("Timestamp the blackout was created in the admin area.");
        }
    }
}
