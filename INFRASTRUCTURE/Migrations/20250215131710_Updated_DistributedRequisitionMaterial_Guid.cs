using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_DistributedRequisitionMaterial_Guid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_Manufacturers_ManufacturerId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_Materials_MaterialId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_RequisitionItems_Requisitio~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Ship~1",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_Suppliers_SupplierId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_WarehouseArrivalLocations_W~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseArrivalLocationId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ShipmentInvoiceItemId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ShipmentInvoiceId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "RequisitionItemId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "MaterialId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ManufacturerId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_Manufacturers_ManufacturerId",
                table: "DistributedRequisitionMaterials",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_Materials_MaterialId",
                table: "DistributedRequisitionMaterials",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_RequisitionItems_Requisitio~",
                table: "DistributedRequisitionMaterials",
                column: "RequisitionItemId",
                principalTable: "RequisitionItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoicesItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Ship~1",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceItemId",
                principalTable: "ShipmentInvoicesItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_Suppliers_SupplierId",
                table: "DistributedRequisitionMaterials",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_WarehouseArrivalLocations_W~",
                table: "DistributedRequisitionMaterials",
                column: "WarehouseArrivalLocationId",
                principalTable: "WarehouseArrivalLocations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_Manufacturers_ManufacturerId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_Materials_MaterialId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_RequisitionItems_Requisitio~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Ship~1",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_Suppliers_SupplierId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_WarehouseArrivalLocations_W~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseArrivalLocationId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ShipmentInvoiceItemId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ShipmentInvoiceId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RequisitionItemId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MaterialId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ManufacturerId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_Manufacturers_ManufacturerId",
                table: "DistributedRequisitionMaterials",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_Materials_MaterialId",
                table: "DistributedRequisitionMaterials",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_RequisitionItems_Requisitio~",
                table: "DistributedRequisitionMaterials",
                column: "RequisitionItemId",
                principalTable: "RequisitionItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoicesItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Ship~1",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceItemId",
                principalTable: "ShipmentInvoicesItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_Suppliers_SupplierId",
                table: "DistributedRequisitionMaterials",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_WarehouseArrivalLocations_W~",
                table: "DistributedRequisitionMaterials",
                column: "WarehouseArrivalLocationId",
                principalTable: "WarehouseArrivalLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
