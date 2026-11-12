using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassAvailabilityMultiTimePerDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                type: "interval",
                nullable: false,
                comment: "Time of day the first occurrence starts each matching day. Classes are always fixed 2-hour segments.",
                oldClrType: typeof(TimeSpan),
                oldType: "interval",
                oldComment: "Time of day each occurrence starts. Classes are always fixed 2-hour segments.");

            migrationBuilder.AddColumn<int>(
                name: "IntervalHours",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "Hours between successive class start times on a matching day. 1 when the class only runs once a day.");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "LastStartTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                comment: "The last class start time of the day. Equals StartTime when the class only runs once a day.");

            // Existing rows only ever had a single start time -- backfill LastStartTime to match
            // StartTime so they keep generating exactly one occurrence per matching day instead of
            // silently dropping to zero (LastStartTime's scaffolded default of 00:00:00 would be
            // before most rules' StartTime, and the expansion loop requires StartTime <= LastStartTime).
            migrationBuilder.Sql(
                "UPDATE potteryjournal.\"ClassAvailabilities\" SET \"LastStartTime\" = \"StartTime\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntervalHours",
                schema: "potteryjournal",
                table: "ClassAvailabilities");

            migrationBuilder.DropColumn(
                name: "LastStartTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                schema: "potteryjournal",
                table: "ClassAvailabilities",
                type: "interval",
                nullable: false,
                comment: "Time of day each occurrence starts. Classes are always fixed 2-hour segments.",
                oldClrType: typeof(TimeSpan),
                oldType: "interval",
                oldComment: "Time of day the first occurrence starts each matching day. Classes are always fixed 2-hour segments.");
        }
    }
}
