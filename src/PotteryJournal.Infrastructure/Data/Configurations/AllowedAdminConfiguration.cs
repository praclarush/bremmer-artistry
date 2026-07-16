using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="AllowedAdmin"/>.
    /// </summary>
    public class AllowedAdminConfiguration : IEntityTypeConfiguration<AllowedAdmin>
    {
        /// <summary>
        /// Configures the entity type builder for <see cref="AllowedAdmin"/>.
        /// </summary>
        /// <param name="builder">The entity type builder supplied by EF Core.</param>
        public void Configure(EntityTypeBuilder<AllowedAdmin> builder)
        {
            builder.ToTable("AllowedAdmins", tb => tb.HasComment("A Google account email permitted to sign in to the admin area."));

            builder.HasKey(a => a.Id).HasName("PK_AllowedAdmins_Id");

            builder.HasAlternateKey(a => a.Email).HasName("UK_AllowedAdmins_Email");

            builder.Property(a => a.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Surrogate primary key.");

            builder.Property(a => a.Email)
                .IsRequired()
                .HasMaxLength(320)
                .HasComment("The allow-listed Google account email.");

            builder.Property(a => a.DisplayName)
                .HasMaxLength(200)
                .HasComment("Optional display name for this admin.");

            builder.Property(a => a.AddedDate)
                .IsRequired()
                .HasDefaultValueSql("now()")
                .HasComment("Timestamp this email was added to the allow-list.");

            builder.Property(a => a.AddedByEmail)
                .HasMaxLength(320)
                .HasComment("Email of the admin who added this entry, if added via the UI.");

            builder.Property(a => a.IsActive)
                .IsRequired()
                .HasDefaultValue(true)
                .HasComment("Whether this email is currently permitted to sign in.");
        }
    }
}
