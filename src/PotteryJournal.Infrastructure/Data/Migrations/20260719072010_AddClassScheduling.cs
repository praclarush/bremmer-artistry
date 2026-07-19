using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlackoutPeriods",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    StartDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Start of the blacked-out range."),
                    EndDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "End of the blacked-out range."),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true, comment: "Optional note explaining why this range is blacked out."),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Timestamp the blackout was created in the admin area.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlackoutPeriods_Id", x => x.Id);
                },
                comment: "An admin-managed date/time range during which class bookings can't be made.");

            migrationBuilder.CreateTable(
                name: "ClassAvailabilities",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    ClassTypeId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The class type this availability window applies to."),
                    StartDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Start date and time of the first/anchor occurrence."),
                    RecurrenceFrequency = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "How often this availability window repeats. None means a single occurrence at StartDateTime."),
                    RecurrenceInterval = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Recurrence step, e.g. 2 with a Weekly frequency means every 2 weeks. Ignored when RecurrenceFrequency is None."),
                    RecurrenceEndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Last date recurrence may occur on. Null means the window recurs indefinitely, bounded only by each read's query range."),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Timestamp the availability window was created in the admin area.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassAvailabilities_Id", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassAvailabilities_ClassTypes_ClassTypeId",
                        column: x => x.ClassTypeId,
                        principalSchema: "potteryjournal",
                        principalTable: "ClassTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "An admin-defined recurring or one-off bookable window for a class type.");

            migrationBuilder.CreateTable(
                name: "ClassBookings",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    ClassTypeId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The class type booked."),
                    StartDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Start date and time of the booked class slot."),
                    EndDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "End date and time of the booked class slot (start plus the fixed 2-hour class duration)."),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Name of the customer who requested the booking."),
                    CustomerEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Email of the customer who requested the booking -- where the confirmation email is sent once approved."),
                    CustomerPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "Optional phone number of the customer."),
                    PartySize = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Number of people in the group, validated against the class type's MaxCapacity."),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Optional note from the customer submitted with the booking request."),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Tentative until an admin approves (Confirmed) or declines (Declined) the request."),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Timestamp the booking request was submitted."),
                    DecisionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Timestamp an admin approved or declined the booking, if a decision has been made.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassBookings_Id", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassBookings_ClassTypes_ClassTypeId",
                        column: x => x.ClassTypeId,
                        principalSchema: "potteryjournal",
                        principalTable: "ClassTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "A customer's request to book a class slot, starting Tentative until an admin approves or declines it.");

            migrationBuilder.CreateIndex(
                name: "IX_BlackoutPeriods_StartDateTime",
                schema: "potteryjournal",
                table: "BlackoutPeriods",
                column: "StartDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_ClassAvailabilities_ClassTypeId",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                column: "ClassTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassAvailabilities_StartDateTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                column: "StartDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_ClassBookings_StartDateTime",
                schema: "potteryjournal",
                table: "ClassBookings",
                column: "StartDateTime");

            migrationBuilder.CreateIndex(
                name: "UK_ClassBookings_ClassTypeId_StartDateTime",
                schema: "potteryjournal",
                table: "ClassBookings",
                columns: new[] { "ClassTypeId", "StartDateTime" },
                unique: true,
                filter: "\"Status\" <> 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlackoutPeriods",
                schema: "potteryjournal");

            migrationBuilder.DropTable(
                name: "ClassAvailabilities",
                schema: "potteryjournal");

            migrationBuilder.DropTable(
                name: "ClassBookings",
                schema: "potteryjournal");
        }
    }
}
