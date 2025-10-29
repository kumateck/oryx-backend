using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class MoveQuantityPerPackToManufacturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityPerPackOption_Max",
                table: "SupplierManufacturers");

            migrationBuilder.DropColumn(
                name: "QuantityPerPackOption_Min",
                table: "SupplierManufacturers");

            migrationBuilder.DropColumn(
                name: "QuantityPerPackOption_Type",
                table: "SupplierManufacturers");

            migrationBuilder.DropColumn(
                name: "QuantityPerPackOption_Values",
                table: "SupplierManufacturers");

            migrationBuilder.DropColumn(
                name: "QuantityType",
                table: "SupplierManufacturers");

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerPackOption_Max",
                table: "ManufacturerMaterials",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerPackOption_Min",
                table: "ManufacturerMaterials",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityPerPackOption_Type",
                table: "ManufacturerMaterials",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<decimal>>(
                name: "QuantityPerPackOption_Values",
                table: "ManufacturerMaterials",
                type: "numeric[]",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityType",
                table: "ManufacturerMaterials",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityPerPackOption_Max",
                table: "ManufacturerMaterials");

            migrationBuilder.DropColumn(
                name: "QuantityPerPackOption_Min",
                table: "ManufacturerMaterials");

            migrationBuilder.DropColumn(
                name: "QuantityPerPackOption_Type",
                table: "ManufacturerMaterials");

            migrationBuilder.DropColumn(
                name: "QuantityPerPackOption_Values",
                table: "ManufacturerMaterials");

            migrationBuilder.DropColumn(
                name: "QuantityType",
                table: "ManufacturerMaterials");

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerPackOption_Max",
                table: "SupplierManufacturers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerPackOption_Min",
                table: "SupplierManufacturers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityPerPackOption_Type",
                table: "SupplierManufacturers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<decimal>>(
                name: "QuantityPerPackOption_Values",
                table: "SupplierManufacturers",
                type: "numeric[]",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityType",
                table: "SupplierManufacturers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
