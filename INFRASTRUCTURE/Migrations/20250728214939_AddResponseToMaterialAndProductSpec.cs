using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseToMaterialAndProductSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<Guid>(
                name: "ResponseId",
                table: "ProductSpecifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResponseId",
                table: "MaterialSpecifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSpecifications_ResponseId",
                table: "ProductSpecifications",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSpecifications_ResponseId",
                table: "MaterialSpecifications",
                column: "ResponseId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSpecifications_Responses_ResponseId",
                table: "MaterialSpecifications",
                column: "ResponseId",
                principalTable: "Responses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSpecifications_Responses_ResponseId",
                table: "ProductSpecifications",
                column: "ResponseId",
                principalTable: "Responses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSpecifications_Responses_ResponseId",
                table: "MaterialSpecifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSpecifications_Responses_ResponseId",
                table: "ProductSpecifications");

            migrationBuilder.DropIndex(
                name: "IX_ProductSpecifications_ResponseId",
                table: "ProductSpecifications");

            migrationBuilder.DropIndex(
                name: "IX_MaterialSpecifications_ResponseId",
                table: "MaterialSpecifications");

            migrationBuilder.DropColumn(
                name: "ResponseId",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "ResponseId",
                table: "MaterialSpecifications");

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
    }
}
