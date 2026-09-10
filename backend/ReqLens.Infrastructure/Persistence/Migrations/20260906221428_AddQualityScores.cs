using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReqLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quality_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Overall = table.Column<int>(type: "integer", nullable: false),
                    Completeness = table.Column<int>(type: "integer", nullable: false),
                    Clarity = table.Column<int>(type: "integer", nullable: false),
                    Testability = table.Column<int>(type: "integer", nullable: false),
                    Consistency = table.Column<int>(type: "integer", nullable: false),
                    Specificity = table.Column<int>(type: "integer", nullable: false),
                    RequirementCount = table.Column<int>(type: "integer", nullable: false),
                    VagueCount = table.Column<int>(type: "integer", nullable: false),
                    VagueScore = table.Column<int>(type: "integer", nullable: false),
                    HasActorCount = table.Column<int>(type: "integer", nullable: false),
                    ActorScore = table.Column<int>(type: "integer", nullable: false),
                    HasMeasurableConstraintCount = table.Column<int>(type: "integer", nullable: false),
                    MeasurableConstraintCoverage = table.Column<int>(type: "integer", nullable: false),
                    UnresolvedContradictionCount = table.Column<int>(type: "integer", nullable: false),
                    InvolvedInContradictionCount = table.Column<int>(type: "integer", nullable: false),
                    UnansweredQuestionCount = table.Column<int>(type: "integer", nullable: false),
                    QuestionScore = table.Column<int>(type: "integer", nullable: false),
                    LlmClarity = table.Column<int>(type: "integer", nullable: false),
                    LlmSpecificity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quality_scores_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quality_scores_DocumentId",
                table: "quality_scores",
                column: "DocumentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quality_scores");
        }
    }
}
