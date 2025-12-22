using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddUoMToSupplierManufacturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UoMId",
                table: "SupplierManufacturers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturers_UoMId",
                table: "SupplierManufacturers",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierManufacturers_UnitOfMeasures_UoMId",
                table: "SupplierManufacturers",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierManufacturers_UnitOfMeasures_UoMId",
                table: "SupplierManufacturers");

            migrationBuilder.DropIndex(
                name: "IX_SupplierManufacturers_UoMId",
                table: "SupplierManufacturers");

            migrationBuilder.DropColumn(
                name: "UoMId",
                table: "SupplierManufacturers");
        }
    }
}
