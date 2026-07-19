using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="ClassBooking"/>.
    /// </summary>
    public class ClassBookingConfiguration : IEntityTypeConfiguration<ClassBooking>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="ClassBooking"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<ClassBooking> builder)
        {
            builder.ToTable("ClassBookings", tb => tb.HasComment("A customer's request to book a class slot, starting Tentative until an admin approves or declines it."));

            builder.HasKey(b => b.Id).HasName("PK_ClassBookings_Id");

            builder.HasIndex(b => b.StartDateTime).HasDatabaseName("IX_ClassBookings_StartDateTime");

            // Enforces "one active (Tentative or Confirmed) booking per class type per slot" at the
            // DB level -- a Declined booking doesn't hold the slot, so a filtered/partial unique
            // index (excluding Declined = 2) rather than a plain unique constraint.
            builder.HasIndex(b => new { b.ClassTypeId, b.StartDateTime })
                .IsUnique()
                .HasFilter("\"Status\" <> 2")
                .HasDatabaseName("UK_ClassBookings_ClassTypeId_StartDateTime");

            builder.Property(b => b.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(b => b.ClassTypeId)
                .IsRequired()
                .HasComment("The class type booked.");

            builder.HasOne(b => b.ClassType)
                .WithMany()
                .HasForeignKey(b => b.ClassTypeId)
                .HasConstraintName("FK_ClassBookings_ClassTypes_ClassTypeId")
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(b => b.StartDateTime)
                .IsRequired()
                .HasComment("Start date and time of the booked class slot.");

            builder.Property(b => b.EndDateTime)
                .IsRequired()
                .HasComment("End date and time of the booked class slot (start plus the fixed 2-hour class duration).");

            builder.Property(b => b.CustomerName)
                .IsRequired()
                .HasMaxLength(200)
                .HasComment("Name of the customer who requested the booking.");

            builder.Property(b => b.CustomerEmail)
                .IsRequired()
                .HasMaxLength(320)
                .HasComment("Email of the customer who requested the booking -- where the confirmation email is sent once approved.");

            builder.Property(b => b.CustomerPhone)
                .HasMaxLength(30)
                .HasComment("Optional phone number of the customer.");

            builder.Property(b => b.PartySize)
                .IsRequired()
                .HasDefaultValue(1)
                .HasComment("Number of people in the group, validated against the class type's MaxCapacity.");

            builder.Property(b => b.Message)
                .HasMaxLength(1000)
                .HasComment("Optional note from the customer submitted with the booking request.");

            builder.Property(b => b.Status)
                .IsRequired()
                .HasDefaultValue(ClassBookingStatus.Tentative)
                .HasComment("Tentative until an admin approves (Confirmed) or declines (Declined) the request.");

            builder.Property(b => b.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("now()")
                .HasComment("Timestamp the booking request was submitted.");

            builder.Property(b => b.DecisionDate)
                .HasComment("Timestamp an admin approved or declined the booking, if a decision has been made.");
        }
    }
}
