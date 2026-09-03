using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class LinkBillingSheetChargePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BillingSheetChargeId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BillingSheetChargeId",
                table: "Payments",
                column: "BillingSheetChargeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_BillingSheetCharges_BillingSheetChargeId",
                table: "Payments",
                column: "BillingSheetChargeId",
                principalTable: "BillingSheetCharges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_BillingSheetCharges_BillingSheetChargeId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_BillingSheetChargeId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BillingSheetChargeId",
                table: "Payments");
        }
    }
}
