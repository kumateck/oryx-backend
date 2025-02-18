using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_MaterialBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_Manufacturers_ManufacturerId",
                table: "Checklists");

            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_Materials_MaterialId",
                table: "Checklists");

            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_ShipmentInvoices_ShipmentInvoiceId",
                table: "Checklists");

            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_Suppliers_SupplierId",
                table: "Checklists");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiryDate",
                table: "MaterialBatches",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "MaterialBatches",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManufacturingDate",
                table: "MaterialBatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetestDate",
                table: "MaterialBatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "Checklists",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ShipmentInvoiceId",
                table: "Checklists",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "MaterialId",
                table: "Checklists",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ManufacturerId",
                table: "Checklists",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_Manufacturers_ManufacturerId",
                table: "Checklists",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_Materials_MaterialId",
                table: "Checklists",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_ShipmentInvoices_ShipmentInvoiceId",
                table: "Checklists",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_Suppliers_SupplierId",
                table: "Checklists",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_Manufacturers_ManufacturerId",
                table: "Checklists");

            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_Materials_MaterialId",
                table: "Checklists");

            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_ShipmentInvoices_ShipmentInvoiceId",
                table: "Checklists");

            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_Suppliers_SupplierId",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "ManufacturingDate",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "RetestDate",
                table: "MaterialBatches");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiryDate",
                table: "MaterialBatches",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "Checklists",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ShipmentInvoiceId",
                table: "Checklists",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MaterialId",
                table: "Checklists",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ManufacturerId",
                table: "Checklists",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_Manufacturers_ManufacturerId",
                table: "Checklists",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_Materials_MaterialId",
                table: "Checklists",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_ShipmentInvoices_ShipmentInvoiceId",
                table: "Checklists",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_Suppliers_SupplierId",
                table: "Checklists",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
