using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="ClassType"/>.
    /// </summary>
    public class ClassTypeConfiguration : IEntityTypeConfiguration<ClassType>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="ClassType"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<ClassType> builder)
        {
            builder.ToTable("ClassTypes", tb => tb.HasComment("Managed list of class type options (e.g. Wheel Throw, Hand-Building) bookable by the public."));

            builder.HasKey(c => c.Id).HasName("PK_ClassTypes_Id");

            builder.HasAlternateKey(c => c.Name).HasName("UK_ClassTypes_Name");

            builder.Property(c => c.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("Display name of the class type, e.g. Wheel Throw.");

            builder.Property(c => c.MaxCapacity)
                .IsRequired()
                .HasDefaultValue(6)
                .HasComment("Maximum party size a single booking may request for this class type.");
        }
    }
}
