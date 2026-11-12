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
            builder.ToTable("ClassAvailabilities", tb => tb.HasComment("An admin-defined weekly bookable window for a class type: which weekdays it's offered on and what time it starts."));

            builder.HasKey(a => a.Id).HasName("PK_ClassAvailabilities_Id");

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

            builder.Property(a => a.DaysOfWeek)
                .IsRequired()
                .HasComment("Bitmask of weekdays this rule is offered on (Sunday=1, Monday=2, Tuesday=4, Wednesday=8, Thursday=16, Friday=32, Saturday=64).");

            builder.Property(a => a.StartTime)
                .IsRequired()
                .HasComment("Time of day the first occurrence starts each matching day. Classes are always fixed 2-hour segments.");

            builder.Property(a => a.LastStartTime)
                .IsRequired()
                .HasComment("The last class start time of the day. Equals StartTime when the class only runs once a day.");

            builder.Property(a => a.IntervalHours)
                .IsRequired()
                .HasDefaultValue(1)
                .HasComment("Hours between successive class start times on a matching day. 1 when the class only runs once a day.");

            builder.Property(a => a.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("now()")
                .HasComment("Timestamp the availability window was created in the admin area.");
        }
    }
}
