using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class BillingSheetCharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Charges_BillingSheets_BillingSheetId",
                table: "Charges");

            migrationBuilder.DropIndex(
                name: "IX_Charges_BillingSheetId",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "BillingSheetId",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "Paid",
                table: "Charges");

            migrationBuilder.CreateTable(
                name: "BillingSheetCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingSheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Paid = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingSheetCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingSheetCharges_BillingSheets_BillingSheetId",
                        column: x => x.BillingSheetId,
                        principalTable: "BillingSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingSheetCharges_Charges_ChargeId",
                        column: x => x.ChargeId,
                        principalTable: "Charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingSheetCharges_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheetCharges_BillingSheetId",
                table: "BillingSheetCharges",
                column: "BillingSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheetCharges_ChargeId",
                table: "BillingSheetCharges",
                column: "ChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheetCharges_LastUpdatedById",
                table: "BillingSheetCharges",
                column: "LastUpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingSheetCharges");

            migrationBuilder.AddColumn<Guid>(
                name: "BillingSheetId",
                table: "Charges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Paid",
                table: "Charges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Charges_BillingSheetId",
                table: "Charges",
                column: "BillingSheetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Charges_BillingSheets_BillingSheetId",
                table: "Charges",
                column: "BillingSheetId",
                principalTable: "BillingSheets",
                principalColumn: "Id");
        }
    }
}
