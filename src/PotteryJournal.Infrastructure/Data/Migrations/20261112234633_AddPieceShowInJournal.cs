using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPieceShowInJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowInJournal",
                schema: "potteryjournal",
                table: "Pieces",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Whether this piece appears on the public Pottery Journal. Unchecked for test tiles and practice pieces that stay in the admin record only.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowInJournal",
                schema: "potteryjournal",
                table: "Pieces");
        }
    }
}
