using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventFlyerAndSocialLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlyerImageFileName",
                schema: "potteryjournal",
                table: "Events",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true,
                comment: "File name of the event's flyer photo on the uploads volume, shown in a lightbox separately from the banner.");

            migrationBuilder.AddColumn<string>(
                name: "SocialMediaUrl",
                schema: "potteryjournal",
                table: "Events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Optional social media link shown on the event card, between the description and the calendar actions.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlyerImageFileName",
                schema: "potteryjournal",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SocialMediaUrl",
                schema: "potteryjournal",
                table: "Events");
        }
    }
}
