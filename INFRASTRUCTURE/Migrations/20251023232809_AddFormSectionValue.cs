using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFormSectionValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Value",
                table: "FormSections");

            migrationBuilder.CreateTable(
                name: "FormSectionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Value = table.Column<string>(type: "character varying(1000000)", maxLength: 1000000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSectionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSectionValues_FormSections_FormSectionId",
                        column: x => x.FormSectionId,
                        principalTable: "FormSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormSectionValues_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormSectionValues_FormSectionId",
                table: "FormSectionValues",
                column: "FormSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSectionValues_MaterialBatchId",
                table: "FormSectionValues",
                column: "MaterialBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormSectionValues");

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "FormSections",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true);
        }
    }
}
