using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddConstraintToApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShipmentDocumentApprovals_ApprovalId",
                table: "ShipmentDocumentApprovals");

            migrationBuilder.DropIndex(
                name: "IX_ResponseApprovals_ApprovalId",
                table: "ResponseApprovals");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionApprovals_ApprovalId",
                table: "RequisitionApprovals");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderApprovals_ApprovalId",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropIndex(
                name: "IX_ProformaInvoiceApprovals_ApprovalId",
                table: "ProformaInvoiceApprovals");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrderApprovals_ApprovalId",
                table: "ProductionOrderApprovals");

            migrationBuilder.DropIndex(
                name: "IX_OvertimeRequestApprovals_ApprovalId",
                table: "OvertimeRequestApprovals");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequestApprovals_ApprovalId",
                table: "LeaveRequestApprovals");

            migrationBuilder.DropIndex(
                name: "IX_BillingSheetApprovals_ApprovalId",
                table: "BillingSheetApprovals");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocumentApprovals_ApprovalId_ShipmentDocumentId_Ord~",
                table: "ShipmentDocumentApprovals",
                columns: new[] { "ApprovalId", "ShipmentDocumentId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseApprovals_ApprovalId_ResponseId_Order_UserId_RoleId",
                table: "ResponseApprovals",
                columns: new[] { "ApprovalId", "ResponseId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_ApprovalId_RequisitionId_Order_UserId_~",
                table: "RequisitionApprovals",
                columns: new[] { "ApprovalId", "RequisitionId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderApprovals_ApprovalId_PurchaseOrderId_Order_Use~",
                table: "PurchaseOrderApprovals",
                columns: new[] { "ApprovalId", "PurchaseOrderId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceApprovals_ApprovalId_ProformaInvoiceId_Order~",
                table: "ProformaInvoiceApprovals",
                columns: new[] { "ApprovalId", "ProformaInvoiceId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderApprovals_ApprovalId_ProductionOrderId_Order~",
                table: "ProductionOrderApprovals",
                columns: new[] { "ApprovalId", "ProductionOrderId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequestApprovals_ApprovalId_OvertimeRequestId_Order~",
                table: "OvertimeRequestApprovals",
                columns: new[] { "ApprovalId", "OvertimeRequestId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestApprovals_ApprovalId_LeaveRequestId_Order_UserI~",
                table: "LeaveRequestApprovals",
                columns: new[] { "ApprovalId", "LeaveRequestId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheetApprovals_ApprovalId_BillingSheetId_Order_UserI~",
                table: "BillingSheetApprovals",
                columns: new[] { "ApprovalId", "BillingSheetId", "Order", "UserId", "RoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShipmentDocumentApprovals_ApprovalId_ShipmentDocumentId_Ord~",
                table: "ShipmentDocumentApprovals");

            migrationBuilder.DropIndex(
                name: "IX_ResponseApprovals_ApprovalId_ResponseId_Order_UserId_RoleId",
                table: "ResponseApprovals");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionApprovals_ApprovalId_RequisitionId_Order_UserId_~",
                table: "RequisitionApprovals");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderApprovals_ApprovalId_PurchaseOrderId_Order_Use~",
                table: "PurchaseOrderApprovals");

            migrationBuilder.DropIndex(
                name: "IX_ProformaInvoiceApprovals_ApprovalId_ProformaInvoiceId_Order~",
                table: "ProformaInvoiceApprovals");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrderApprovals_ApprovalId_ProductionOrderId_Order~",
                table: "ProductionOrderApprovals");

            migrationBuilder.DropIndex(
                name: "IX_OvertimeRequestApprovals_ApprovalId_OvertimeRequestId_Order~",
                table: "OvertimeRequestApprovals");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequestApprovals_ApprovalId_LeaveRequestId_Order_UserI~",
                table: "LeaveRequestApprovals");

            migrationBuilder.DropIndex(
                name: "IX_BillingSheetApprovals_ApprovalId_BillingSheetId_Order_UserI~",
                table: "BillingSheetApprovals");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocumentApprovals_ApprovalId",
                table: "ShipmentDocumentApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseApprovals_ApprovalId",
                table: "ResponseApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_ApprovalId",
                table: "RequisitionApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderApprovals_ApprovalId",
                table: "PurchaseOrderApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoiceApprovals_ApprovalId",
                table: "ProformaInvoiceApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderApprovals_ApprovalId",
                table: "ProductionOrderApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequestApprovals_ApprovalId",
                table: "OvertimeRequestApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestApprovals_ApprovalId",
                table: "LeaveRequestApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheetApprovals_ApprovalId",
                table: "BillingSheetApprovals",
                column: "ApprovalId");
        }
    }
}
