using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReqLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAmbiguities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ambiguities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Issue = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ambiguities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ambiguities_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ambiguity_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AmbiguityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    OptionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ambiguity_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ambiguity_questions_ambiguities_AmbiguityId",
                        column: x => x.AmbiguityId,
                        principalTable: "ambiguities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ambiguities_DocumentId",
                table: "ambiguities",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ambiguity_questions_AmbiguityId",
                table: "ambiguity_questions",
                column: "AmbiguityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ambiguity_questions");

            migrationBuilder.DropTable(
                name: "ambiguities");
        }
    }
}
