using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShipmentDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentDocuments_PurchaseOrders_PurchaseOrderId",
                table: "ShipmentDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoices_ShipmentDocuments_ShipmentDocumentId",
                table: "ShipmentInvoices");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentInvoices_ShipmentDocumentId",
                table: "ShipmentInvoices");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentDocuments_PurchaseOrderId",
                table: "ShipmentDocuments");

            migrationBuilder.DropColumn(
                name: "ShipmentDocumentId",
                table: "ShipmentInvoices");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "ShipmentDocuments");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "ShipmentDocuments");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ShipmentInvoices",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "PurchaseOrderIds",
                table: "ShipmentInvoices",
                type: "uuid[]",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentInvoiceId",
                table: "ShipmentDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_ShipmentInvoiceId",
                table: "ShipmentDocuments",
                column: "ShipmentInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentDocuments_ShipmentInvoices_ShipmentInvoiceId",
                table: "ShipmentDocuments",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentDocuments_ShipmentInvoices_ShipmentInvoiceId",
                table: "ShipmentDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentDocuments_ShipmentInvoiceId",
                table: "ShipmentDocuments");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ShipmentInvoices");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderIds",
                table: "ShipmentInvoices");

            migrationBuilder.DropColumn(
                name: "ShipmentInvoiceId",
                table: "ShipmentDocuments");

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentDocumentId",
                table: "ShipmentInvoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "ShipmentDocuments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "ShipmentDocuments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_ShipmentDocumentId",
                table: "ShipmentInvoices",
                column: "ShipmentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_PurchaseOrderId",
                table: "ShipmentDocuments",
                column: "PurchaseOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentDocuments_PurchaseOrders_PurchaseOrderId",
                table: "ShipmentDocuments",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoices_ShipmentDocuments_ShipmentDocumentId",
                table: "ShipmentInvoices",
                column: "ShipmentDocumentId",
                principalTable: "ShipmentDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
