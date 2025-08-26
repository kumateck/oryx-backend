using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductPacking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "ProductPackingList");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ProductPackings",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ProductPackings",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UomId",
                table: "ProductPackingList",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackingList_UomId",
                table: "ProductPackingList",
                column: "UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackingList_UnitOfMeasures_UomId",
                table: "ProductPackingList",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackingList_UnitOfMeasures_UomId",
                table: "ProductPackingList");

            migrationBuilder.DropIndex(
                name: "IX_ProductPackingList_UomId",
                table: "ProductPackingList");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ProductPackings");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ProductPackings");

            migrationBuilder.DropColumn(
                name: "UomId",
                table: "ProductPackingList");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "ProductPackingList",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);
        }
    }
}
