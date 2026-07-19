using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="AdminSettings"/>.
    /// </summary>
    public class AdminSettingsConfiguration : IEntityTypeConfiguration<AdminSettings>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="AdminSettings"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<AdminSettings> builder)
        {
            builder.ToTable("AdminSettings", tb => tb.HasComment("Studio-wide settings editable by an admin without a redeploy. Always exactly one row."));

            builder.HasKey(s => s.Id).HasName("PK_AdminSettings_Id");

            builder.Property(s => s.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(s => s.NotificationRecipientEmail)
                .IsRequired()
                .HasMaxLength(320)
                .HasComment("Email address that receives class booking and Contact Us notifications.");

            builder.Property(s => s.MinimumBookingLeadDays)
                .IsRequired()
                .HasDefaultValue(2)
                .HasComment("Minimum number of days in advance a class booking must be requested.");
        }
    }
}
