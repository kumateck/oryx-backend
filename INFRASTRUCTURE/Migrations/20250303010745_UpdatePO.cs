using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_SourceRequisitions_SourceRequisitionId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierQuotations_SourceRequisitions_SourceRequisitionId",
                table: "SupplierQuotations");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceRequisitionId",
                table: "SupplierQuotations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceRequisitionId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_SourceRequisitions_SourceRequisitionId",
                table: "PurchaseOrders",
                column: "SourceRequisitionId",
                principalTable: "SourceRequisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierQuotations_SourceRequisitions_SourceRequisitionId",
                table: "SupplierQuotations",
                column: "SourceRequisitionId",
                principalTable: "SourceRequisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_SourceRequisitions_SourceRequisitionId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierQuotations_SourceRequisitions_SourceRequisitionId",
                table: "SupplierQuotations");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceRequisitionId",
                table: "SupplierQuotations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceRequisitionId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_SourceRequisitions_SourceRequisitionId",
                table: "PurchaseOrders",
                column: "SourceRequisitionId",
                principalTable: "SourceRequisitions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierQuotations_SourceRequisitions_SourceRequisitionId",
                table: "SupplierQuotations",
                column: "SourceRequisitionId",
                principalTable: "SourceRequisitions",
                principalColumn: "Id");
        }
    }
}
