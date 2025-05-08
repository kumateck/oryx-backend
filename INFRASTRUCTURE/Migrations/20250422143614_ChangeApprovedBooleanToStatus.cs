using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeApprovedBooleanToStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Approved",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "BillingSheetApprovals");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "RequisitionApprovals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PurchaseOrderApprovals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "BillingSheetApprovals",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "RequisitionApprovals");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BillingSheetApprovals");

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "RequisitionApprovals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "PurchaseOrderApprovals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "BillingSheetApprovals",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
