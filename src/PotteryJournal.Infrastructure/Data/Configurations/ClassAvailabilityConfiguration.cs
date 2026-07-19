using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="ClassAvailability"/>.
    /// </summary>
    public class ClassAvailabilityConfiguration : IEntityTypeConfiguration<ClassAvailability>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="ClassAvailability"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<ClassAvailability> builder)
        {
            builder.ToTable("ClassAvailabilities", tb => tb.HasComment("An admin-defined recurring or one-off bookable window for a class type."));

            builder.HasKey(a => a.Id).HasName("PK_ClassAvailabilities_Id");

            builder.HasIndex(a => a.StartDateTime).HasDatabaseName("IX_ClassAvailabilities_StartDateTime");

            builder.Property(a => a.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(a => a.ClassTypeId)
                .IsRequired()
                .HasComment("The class type this availability window applies to.");

            builder.HasOne(a => a.ClassType)
                .WithMany()
                .HasForeignKey(a => a.ClassTypeId)
                .HasConstraintName("FK_ClassAvailabilities_ClassTypes_ClassTypeId")
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(a => a.StartDateTime)
                .IsRequired()
                .HasComment("Start date and time of the first/anchor occurrence.");

            builder.Property(a => a.RecurrenceFrequency)
                .IsRequired()
                .HasDefaultValue(RecurrenceFrequency.None)
                .HasComment("How often this availability window repeats. None means a single occurrence at StartDateTime.");

            builder.Property(a => a.RecurrenceInterval)
                .IsRequired()
                .HasDefaultValue(1)
                .HasComment("Recurrence step, e.g. 2 with a Weekly frequency means every 2 weeks. Ignored when RecurrenceFrequency is None.");

            builder.Property(a => a.RecurrenceEndDate)
                .HasComment("Last date recurrence may occur on. Null means the window recurs indefinitely, bounded only by each read's query range.");

            builder.Property(a => a.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("now()")
                .HasComment("Timestamp the availability window was created in the admin area.");
        }
    }
}
