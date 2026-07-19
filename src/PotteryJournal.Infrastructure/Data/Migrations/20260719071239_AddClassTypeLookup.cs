using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassTypeLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassTypes",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Display name of the class type, e.g. Wheel Throw."),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: false, defaultValue: 6, comment: "Maximum party size a single booking may request for this class type.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassTypes_Id", x => x.Id);
                    table.UniqueConstraint("UK_ClassTypes_Name", x => x.Name);
                },
                comment: "Managed list of class type options (e.g. Wheel Throw, Hand-Building) bookable by the public.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassTypes",
                schema: "potteryjournal");
        }
    }
}
