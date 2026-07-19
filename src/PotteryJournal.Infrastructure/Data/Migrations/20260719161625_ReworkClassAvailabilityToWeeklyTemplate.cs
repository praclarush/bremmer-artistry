using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReworkClassAvailabilityToWeeklyTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClassAvailabilities_StartDateTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                schema: "potteryjournal",
                table: "ClassAvailabilities");

            migrationBuilder.DropColumn(
                name: "RecurrenceFrequency",
                schema: "potteryjournal",
                table: "ClassAvailabilities");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                schema: "potteryjournal",
                table: "ClassAvailabilities");

            migrationBuilder.DropColumn(
                name: "StartDateTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities");

            migrationBuilder.AlterTable(
                name: "ClassAvailabilities",
                schema: "potteryjournal",
                comment: "An admin-defined weekly bookable window for a class type: which weekdays it's offered on and what time it starts.",
                oldComment: "An admin-defined recurring or one-off bookable window for a class type.");

            migrationBuilder.AddColumn<int>(
                name: "DaysOfWeek",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Bitmask of weekdays this rule is offered on (Sunday=1, Monday=2, Tuesday=4, Wednesday=8, Thursday=16, Friday=32, Saturday=64).");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                comment: "Time of day each occurrence starts. Classes are always fixed 2-hour segments.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaysOfWeek",
                schema: "potteryjournal",
                table: "ClassAvailabilities");

            migrationBuilder.DropColumn(
                name: "StartTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities");

            migrationBuilder.AlterTable(
                name: "ClassAvailabilities",
                schema: "potteryjournal",
                comment: "An admin-defined recurring or one-off bookable window for a class type.",
                oldComment: "An admin-defined weekly bookable window for a class type: which weekdays it's offered on and what time it starts.");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RecurrenceEndDate",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Last date recurrence may occur on. Null means the window recurs indefinitely, bounded only by each read's query range.");

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceFrequency",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "How often this availability window repeats. None means a single occurrence at StartDateTime.");

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "Recurrence step, e.g. 2 with a Weekly frequency means every 2 weeks. Ignored when RecurrenceFrequency is None.");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDateTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                comment: "Start date and time of the first/anchor occurrence.");

            migrationBuilder.CreateIndex(
                name: "IX_ClassAvailabilities_StartDateTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                column: "StartDateTime");
        }
    }
}
