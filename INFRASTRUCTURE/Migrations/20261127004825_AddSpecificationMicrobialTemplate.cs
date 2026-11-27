using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificationMicrobialTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MicrobialFormId",
                table: "ProductSpecifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MicrobialFormId",
                table: "MaterialSpecifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSpecifications_MicrobialFormId",
                table: "ProductSpecifications",
                column: "MicrobialFormId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSpecifications_MicrobialFormId",
                table: "MaterialSpecifications",
                column: "MicrobialFormId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSpecifications_Forms_MicrobialFormId",
                table: "MaterialSpecifications",
                column: "MicrobialFormId",
                principalTable: "Forms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSpecifications_Forms_MicrobialFormId",
                table: "ProductSpecifications",
                column: "MicrobialFormId",
                principalTable: "Forms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSpecifications_Forms_MicrobialFormId",
                table: "MaterialSpecifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSpecifications_Forms_MicrobialFormId",
                table: "ProductSpecifications");

            migrationBuilder.DropIndex(
                name: "IX_ProductSpecifications_MicrobialFormId",
                table: "ProductSpecifications");

            migrationBuilder.DropIndex(
                name: "IX_MaterialSpecifications_MicrobialFormId",
                table: "MaterialSpecifications");

            migrationBuilder.DropColumn(
                name: "MicrobialFormId",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "MicrobialFormId",
                table: "MaterialSpecifications");
        }
    }
}
