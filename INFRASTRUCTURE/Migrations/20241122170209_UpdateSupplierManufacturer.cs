using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSupplierManufacturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialId",
                table: "SupplierManufacturers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturers_MaterialId",
                table: "SupplierManufacturers",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierManufacturers_Materials_MaterialId",
                table: "SupplierManufacturers",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierManufacturers_Materials_MaterialId",
                table: "SupplierManufacturers");

            migrationBuilder.DropIndex(
                name: "IX_SupplierManufacturers_MaterialId",
                table: "SupplierManufacturers");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "SupplierManufacturers");
        }
    }
}
