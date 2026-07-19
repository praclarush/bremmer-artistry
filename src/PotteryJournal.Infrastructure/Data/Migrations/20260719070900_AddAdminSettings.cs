using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminSettings",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    NotificationRecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Email address that receives class booking and Contact Us notifications."),
                    MinimumBookingLeadDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 2, comment: "Minimum number of days in advance a class booking must be requested.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminSettings_Id", x => x.Id);
                },
                comment: "Studio-wide settings editable by an admin without a redeploy. Always exactly one row.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminSettings",
                schema: "potteryjournal");
        }
    }
}
