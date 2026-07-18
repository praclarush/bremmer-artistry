using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClayGlazeCategoryLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the lookup tables, empty for now.
            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Display name of the category, e.g. Bowls.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories_Id", x => x.Id);
                    table.UniqueConstraint("UK_Categories_Name", x => x.Name);
                },
                comment: "Managed list of category options assignable to a piece, used to group it on the Gallery page.");

            migrationBuilder.CreateTable(
                name: "ClayBodies",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Display name of the clay body, e.g. Reclaim.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClayBodies_Id", x => x.Id);
                    table.UniqueConstraint("UK_ClayBodies_Name", x => x.Name);
                },
                comment: "Managed list of clay body options assignable to a piece.");

            migrationBuilder.CreateTable(
                name: "Glazes",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Display name of the glaze, e.g. Waterfall.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Glazes_Id", x => x.Id);
                    table.UniqueConstraint("UK_Glazes_Name", x => x.Name);
                },
                comment: "Managed list of glaze options assignable to a glaze application.");

            // 2. Add the new FK columns alongside the old free-text ones. GlazeId starts nullable
            // so the backfill below can populate it before it's tightened to NOT NULL.
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                schema: "potteryjournal",
                table: "Pieces",
                type: "uuid",
                nullable: true,
                comment: "Managed category assigned to this piece, used to group it on the Gallery page when ShowInGallery is set.");

            migrationBuilder.AddColumn<Guid>(
                name: "ClayBodyId",
                schema: "potteryjournal",
                table: "Pieces",
                type: "uuid",
                nullable: true,
                comment: "Managed clay body assigned to this piece, if known.");

            migrationBuilder.AddColumn<Guid>(
                name: "GlazeId",
                schema: "potteryjournal",
                table: "GlazeApplications",
                type: "uuid",
                nullable: true,
                comment: "The managed glaze used for this application.");

            // 3. Populate the lookup tables from the distinct values already in use. "—" and blank
            // are the existing "not recorded" sentinels for Category/Clay -- they become NULL FK
            // (unspecified), not a literal lookup row.
            migrationBuilder.Sql(@"
                INSERT INTO potteryjournal.""Categories"" (""Id"", ""Name"")
                SELECT gen_random_uuid(), c.""Name""
                FROM (SELECT DISTINCT ""Category"" AS ""Name"" FROM potteryjournal.""Pieces""
                      WHERE ""Category"" IS NOT NULL AND ""Category"" <> '') AS c;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO potteryjournal.""ClayBodies"" (""Id"", ""Name"")
                SELECT gen_random_uuid(), c.""Name""
                FROM (SELECT DISTINCT ""Clay"" AS ""Name"" FROM potteryjournal.""Pieces""
                      WHERE ""Clay"" IS NOT NULL AND ""Clay"" <> '' AND ""Clay"" <> '—') AS c;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO potteryjournal.""Glazes"" (""Id"", ""Name"")
                SELECT gen_random_uuid(), g.""Name""
                FROM (SELECT DISTINCT ""GlazeName"" AS ""Name"" FROM potteryjournal.""GlazeApplications""
                      WHERE ""GlazeName"" IS NOT NULL AND ""GlazeName"" <> '') AS g;
            ");

            // 4. Backfill the FK columns by matching the old free-text values to the new rows.
            migrationBuilder.Sql(@"
                UPDATE potteryjournal.""Pieces"" p
                SET ""CategoryId"" = c.""Id""
                FROM potteryjournal.""Categories"" c
                WHERE c.""Name"" = p.""Category"";
            ");

            migrationBuilder.Sql(@"
                UPDATE potteryjournal.""Pieces"" p
                SET ""ClayBodyId"" = cb.""Id""
                FROM potteryjournal.""ClayBodies"" cb
                WHERE cb.""Name"" = p.""Clay"";
            ");

            migrationBuilder.Sql(@"
                UPDATE potteryjournal.""GlazeApplications"" g
                SET ""GlazeId"" = gl.""Id""
                FROM potteryjournal.""Glazes"" gl
                WHERE gl.""Name"" = g.""GlazeName"";
            ");

            // 5. Now safe to drop the old free-text columns -- their data has been migrated.
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.DropColumn(
                name: "Clay",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.DropColumn(
                name: "GlazeName",
                schema: "potteryjournal",
                table: "GlazeApplications");

            // 6. Every existing GlazeApplication had a non-empty GlazeName, so the backfill above
            // guarantees no NULLs remain -- safe to tighten to NOT NULL.
            migrationBuilder.AlterColumn<Guid>(
                name: "GlazeId",
                schema: "potteryjournal",
                table: "GlazeApplications",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The managed glaze used for this application.",
                comment: "The managed glaze used for this application.");

            migrationBuilder.CreateIndex(
                name: "IX_Pieces_CategoryId",
                schema: "potteryjournal",
                table: "Pieces",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Pieces_ClayBodyId",
                schema: "potteryjournal",
                table: "Pieces",
                column: "ClayBodyId");

            migrationBuilder.CreateIndex(
                name: "IX_GlazeApplications_GlazeId",
                schema: "potteryjournal",
                table: "GlazeApplications",
                column: "GlazeId");

            migrationBuilder.AddForeignKey(
                name: "FK_GlazeApplications_Glazes_GlazeId",
                schema: "potteryjournal",
                table: "GlazeApplications",
                column: "GlazeId",
                principalSchema: "potteryjournal",
                principalTable: "Glazes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pieces_Categories_CategoryId",
                schema: "potteryjournal",
                table: "Pieces",
                column: "CategoryId",
                principalSchema: "potteryjournal",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Pieces_ClayBodies_ClayBodyId",
                schema: "potteryjournal",
                table: "Pieces",
                column: "ClayBodyId",
                principalSchema: "potteryjournal",
                principalTable: "ClayBodies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GlazeApplications_Glazes_GlazeId",
                schema: "potteryjournal",
                table: "GlazeApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_Pieces_Categories_CategoryId",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.DropForeignKey(
                name: "FK_Pieces_ClayBodies_ClayBodyId",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.DropIndex(
                name: "IX_Pieces_CategoryId",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.DropIndex(
                name: "IX_Pieces_ClayBodyId",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.DropIndex(
                name: "IX_GlazeApplications_GlazeId",
                schema: "potteryjournal",
                table: "GlazeApplications");

            // Re-add the old free-text columns and backfill them from the lookup tables before the
            // lookup tables themselves and the FK columns are dropped.
            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "potteryjournal",
                table: "Pieces",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Free-text category tag used to build the Gallery page's category tiles.");

            migrationBuilder.AddColumn<string>(
                name: "Clay",
                schema: "potteryjournal",
                table: "Pieces",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                comment: "Clay body used for the piece.");

            migrationBuilder.AddColumn<string>(
                name: "GlazeName",
                schema: "potteryjournal",
                table: "GlazeApplications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                comment: "Name of the glaze used.");

            migrationBuilder.Sql(@"
                UPDATE potteryjournal.""Pieces"" p
                SET ""Category"" = c.""Name""
                FROM potteryjournal.""Categories"" c
                WHERE c.""Id"" = p.""CategoryId"";
            ");

            migrationBuilder.Sql(@"
                UPDATE potteryjournal.""Pieces"" p
                SET ""Clay"" = cb.""Name""
                FROM potteryjournal.""ClayBodies"" cb
                WHERE cb.""Id"" = p.""ClayBodyId"";
            ");

            migrationBuilder.Sql(@"
                UPDATE potteryjournal.""GlazeApplications"" g
                SET ""GlazeName"" = gl.""Name""
                FROM potteryjournal.""Glazes"" gl
                WHERE gl.""Id"" = g.""GlazeId"";
            ");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.DropColumn(
                name: "ClayBodyId",
                schema: "potteryjournal",
                table: "Pieces");

            migrationBuilder.DropColumn(
                name: "GlazeId",
                schema: "potteryjournal",
                table: "GlazeApplications");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "potteryjournal");

            migrationBuilder.DropTable(
                name: "ClayBodies",
                schema: "potteryjournal");

            migrationBuilder.DropTable(
                name: "Glazes",
                schema: "potteryjournal");
        }
    }
}
