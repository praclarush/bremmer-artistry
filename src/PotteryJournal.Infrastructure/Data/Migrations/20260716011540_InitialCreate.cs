using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PotteryJournal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "potteryjournal");

            migrationBuilder.CreateTable(
                name: "AllowedAdmins",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "The allow-listed Google account email."),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Optional display name for this admin."),
                    AddedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Timestamp this email was added to the allow-list."),
                    AddedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true, comment: "Email of the admin who added this entry, if added via the UI."),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Whether this email is currently permitted to sign in.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowedAdmins_Id", x => x.Id);
                    table.UniqueConstraint("UK_AllowedAdmins_Email", x => x.Email);
                },
                comment: "A Google account email permitted to sign in to the admin area.");

            migrationBuilder.CreateTable(
                name: "Events",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Event title."),
                    Description = table.Column<string>(type: "text", nullable: false, comment: "Event description shown on the card and detail view."),
                    StartDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Event start date and time."),
                    EndDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Event end date and time, if known."),
                    VenueName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Venue name shown on the event card."),
                    VenueAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true, comment: "Venue address, used for the card's map link."),
                    ImageFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true, comment: "File name of the event's banner photo on the uploads volume."),
                    ExternalLinkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Optional external link, e.g. a ticketing or host page."),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Timestamp the event was created in the admin area."),
                    CreatedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Email of the admin who created the event.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events_Id", x => x.Id);
                },
                comment: "A public event shown on the Events page and calendar.");

            migrationBuilder.CreateTable(
                name: "Pieces",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    PieceNumber = table.Column<int>(type: "integer", nullable: false, comment: "Sequential, human-facing project number shown as e.g. #003."),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Display title of the piece."),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Free-text category tag used to build the Gallery page's category tiles."),
                    Clay = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Clay body used for the piece."),
                    StartedDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Date work on the piece began."),
                    FinishedDate = table.Column<DateOnly>(type: "date", nullable: true, comment: "Date the piece was finished, if complete."),
                    SizeText = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Free-text dimensions, e.g. 6\" x 4\"."),
                    WeightText = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Free-text weight of the piece."),
                    GlazeSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Short summary of the glaze treatment shown on the worksheet."),
                    AttachmentsText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Free-text note of any physical attachments recorded for the piece."),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Timestamp the entry was created in the admin area."),
                    CreatedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Email of the admin who created the entry.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pieces_Id", x => x.Id);
                    table.UniqueConstraint("UK_Pieces_PieceNumber", x => x.PieceNumber);
                },
                comment: "A pottery piece entry in the Pottery Journal catalog.");

            migrationBuilder.CreateTable(
                name: "GlazeApplications",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    PieceId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The piece this glaze application belongs to."),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Where on the piece the glaze was applied, e.g. Interior."),
                    GlazeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Name of the glaze used."),
                    Coats = table.Column<int>(type: "integer", nullable: false, comment: "Number of coats applied.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlazeApplications_Id", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlazeApplications_Pieces_PieceId",
                        column: x => x.PieceId,
                        principalSchema: "potteryjournal",
                        principalTable: "Pieces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "A glaze application recorded against a pottery piece.");

            migrationBuilder.CreateTable(
                name: "PieceImages",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    PieceId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The piece this photo belongs to."),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false, comment: "File name of the resized, re-encoded photo on the uploads volume."),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, comment: "Display order of this photo among a piece's photos."),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Timestamp the photo was uploaded.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PieceImages_Id", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PieceImages_Pieces_PieceId",
                        column: x => x.PieceId,
                        principalSchema: "potteryjournal",
                        principalTable: "Pieces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "An uploaded photo belonging to a pottery piece.");

            migrationBuilder.CreateTable(
                name: "PieceNotes",
                schema: "potteryjournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Surrogate primary key."),
                    PieceId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The piece this note belongs to."),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Optional note heading, e.g. a technique name."),
                    NoteText = table.Column<string>(type: "text", nullable: false, comment: "The note body text."),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, comment: "Display order of this note among a piece's notes.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PieceNotes_Id", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PieceNotes_Pieces_PieceId",
                        column: x => x.PieceId,
                        principalSchema: "potteryjournal",
                        principalTable: "Pieces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "A note recorded against a pottery piece.");

            migrationBuilder.CreateIndex(
                name: "IX_Events_StartDateTime",
                schema: "potteryjournal",
                table: "Events",
                column: "StartDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_GlazeApplications_PieceId",
                schema: "potteryjournal",
                table: "GlazeApplications",
                column: "PieceId");

            migrationBuilder.CreateIndex(
                name: "IX_PieceImages_PieceId",
                schema: "potteryjournal",
                table: "PieceImages",
                column: "PieceId");

            migrationBuilder.CreateIndex(
                name: "IX_PieceNotes_PieceId",
                schema: "potteryjournal",
                table: "PieceNotes",
                column: "PieceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllowedAdmins",
                schema: "potteryjournal");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "potteryjournal");

            migrationBuilder.DropTable(
                name: "GlazeApplications",
                schema: "potteryjournal");

            migrationBuilder.DropTable(
                name: "PieceImages",
                schema: "potteryjournal");

            migrationBuilder.DropTable(
                name: "PieceNotes",
                schema: "potteryjournal");

            migrationBuilder.DropTable(
                name: "Pieces",
                schema: "potteryjournal");
        }
    }
}
