using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialAndProductSpecToForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialSpecificationId",
                table: "Responses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductSpecificationId",
                table: "Responses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Responses_MaterialSpecificationId",
                table: "Responses",
                column: "MaterialSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Responses_ProductSpecificationId",
                table: "Responses",
                column: "ProductSpecificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_MaterialSpecifications_MaterialSpecificationId",
                table: "Responses",
                column: "MaterialSpecificationId",
                principalTable: "MaterialSpecifications",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_ProductSpecifications_ProductSpecificationId",
                table: "Responses",
                column: "ProductSpecificationId",
                principalTable: "ProductSpecifications",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Responses_MaterialSpecifications_MaterialSpecificationId",
                table: "Responses");

            migrationBuilder.DropForeignKey(
                name: "FK_Responses_ProductSpecifications_ProductSpecificationId",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_Responses_MaterialSpecificationId",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_Responses_ProductSpecificationId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "MaterialSpecificationId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "ProductSpecificationId",
                table: "Responses");
        }
    }
}
