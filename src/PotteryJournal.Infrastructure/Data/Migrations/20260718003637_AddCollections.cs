using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                schema: "potteryjournal",
                table: "Pieces",
                type: "uuid",
                nullable: true,
                comment: "Managed collection this piece belongs to, if any. Independent of Category.");

            migrationBuilder.CreateTable(
                name: "Collections",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Display name of the collection, e.g. Lightning-Cracked Collection."),
                    IsFeaturedOnHomepage = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether this collection's pieces are shown in the homepage's rotating display. At most one collection is featured at a time.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections_Id", x => x.Id);
                    table.UniqueConstraint("UK_Collections_Name", x => x.Name);
                },
                comment: "A named grouping of pieces, independent of Category. At most one is featured on the homepage.");

            migrationBuilder.CreateIndex(
                name: "IX_Pieces_CollectionId",
                schema: "potteryjournal",
                table: "Pieces",
                column: "CollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pieces_Collections_CollectionId",
                schema: "potteryjournal",
                table: "Pieces",
                column: "CollectionId",
                principalSchema: "potteryjournal",
                principalTable: "Collections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // The four "___ Collection" categories were a distinct taxonomy in the original source
            // photos (a separate "Collections" subfolder, not a form-type category) that got
            // flattened into Category during the initial import. Move them into real Collections.
            migrationBuilder.Sql(@"
                INSERT INTO potteryjournal.""Collections"" (""Id"", ""Name"")
                SELECT gen_random_uuid(), c.""Name""
                FROM potteryjournal.""Categories"" c
                WHERE c.""Name"" IN ('Carved Collection', 'Iron-Fall Blue Collection', 'Lightning-Cracked Collection', 'Peeled Collection');
            ");

            migrationBuilder.Sql(@"
                UPDATE potteryjournal.""Pieces"" p
                SET ""CollectionId"" = col.""Id""
                FROM potteryjournal.""Categories"" cat
                JOIN potteryjournal.""Collections"" col ON col.""Name"" = cat.""Name""
                WHERE p.""CategoryId"" = cat.""Id""
                  AND cat.""Name"" IN ('Carved Collection', 'Iron-Fall Blue Collection', 'Lightning-Cracked Collection', 'Peeled Collection');
            ");

            migrationBuilder.Sql(@"
                UPDATE potteryjournal.""Pieces""
                SET ""CategoryId"" = NULL
                WHERE ""CategoryId"" IN (
                    SELECT ""Id"" FROM potteryjournal.""Categories""
                    WHERE ""Name"" IN ('Carved Collection', 'Iron-Fall Blue Collection', 'Lightning-Cracked Collection', 'Peeled Collection')
                );
            ");

            migrationBuilder.Sql(@"
                DELETE FROM potteryjournal.""Categories""
                WHERE ""Name"" IN ('Carved Collection', 'Iron-Fall Blue Collection', 'Lightning-Cracked Collection', 'Peeled Collection');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the four collections as categories before the Collections table is dropped.
            migrationBuilder.Sql(@"
                INSERT INTO potteryjournal.""Categories"" (""Id"", ""Name"")
                SELECT gen_random_uuid(), col.""Name""
                FROM potteryjournal.""Collections"" col
                WHERE col.""Name"" IN ('Carved Collection', 'Iron-Fall Blue Collection', 'Lightning-Cracked Collection', 'Peeled Collection')
                  AND NOT EXISTS (SELECT 1 FROM potteryjournal.""Categories"" cat WHERE cat.""Name"" = col.""Name"");
            ");

            migrationBuilder.Sql(@"
                UPDATE potteryjournal.""Pieces"" p
                SET ""CategoryId"" = cat.""Id""
                FROM potteryjournal.""Collections"" col
                JOIN potteryjournal.""Categories"" cat ON cat.""Name"" = col.""Name""
                WHERE p.""CollectionId"" = col.""Id""
                  AND col.""Name"" IN ('Carved Collection', 'Iron-Fall Blue Collection', 'Lightning-Cracked Collection', 'Peeled Collection');
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Pieces_Collections_CollectionId",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.DropTable(
                name: "Collections",
                schema: "potteryjournal");

            migrationBuilder.DropIndex(
                name: "IX_Pieces_CollectionId",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                schema: "potteryjournal",
                table: "Pieces");
        }
    }
}
