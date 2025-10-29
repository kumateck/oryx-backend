using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSpecToFormSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductSpecificationId",
                table: "FormSections",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_ProductSpecificationId",
                table: "FormSections",
                column: "ProductSpecificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormSections_ProductSpecifications_ProductSpecificationId",
                table: "FormSections",
                column: "ProductSpecificationId",
                principalTable: "ProductSpecifications",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormSections_ProductSpecifications_ProductSpecificationId",
                table: "FormSections");

            migrationBuilder.DropIndex(
                name: "IX_FormSections_ProductSpecificationId",
                table: "FormSections");

            migrationBuilder.DropColumn(
                name: "ProductSpecificationId",
                table: "FormSections");
        }
    }
}
