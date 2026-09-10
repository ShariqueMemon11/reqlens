using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReqLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContradictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contradictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementIdA = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequirementIdB = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ResolutionQuestion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contradictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contradictions_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contradictions_DocumentId",
                table: "contradictions",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contradictions");
        }
    }
}
