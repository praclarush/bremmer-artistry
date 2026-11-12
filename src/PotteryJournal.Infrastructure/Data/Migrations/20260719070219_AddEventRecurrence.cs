using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RecurrenceEndDate",
                schema: "potteryjournal",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Last date recurrence may occur on. Null means the event recurs indefinitely, bounded only by each read's query range.");

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceFrequency",
                schema: "potteryjournal",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "How often this event repeats. None means a single occurrence at StartDateTime.");

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                schema: "potteryjournal",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "Recurrence step, e.g. 2 with a Weekly frequency means every 2 weeks. Ignored when RecurrenceFrequency is None.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                schema: "potteryjournal",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RecurrenceFrequency",
                schema: "potteryjournal",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                schema: "potteryjournal",
                table: "Events");
        }
    }
}
