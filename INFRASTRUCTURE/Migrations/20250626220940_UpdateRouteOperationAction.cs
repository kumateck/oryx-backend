using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRouteOperationAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteOperationAction_Forms_FormId",
                table: "RouteOperationAction");

            migrationBuilder.RenameColumn(
                name: "FormId",
                table: "RouteOperationAction",
                newName: "ProductAnalyticalRawDataId");

            migrationBuilder.RenameIndex(
                name: "IX_RouteOperationAction_FormId",
                table: "RouteOperationAction",
                newName: "IX_RouteOperationAction_ProductAnalyticalRawDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteOperationAction_ProductAnalyticalRawData_ProductAnalyt~",
                table: "RouteOperationAction",
                column: "ProductAnalyticalRawDataId",
                principalTable: "ProductAnalyticalRawData",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteOperationAction_ProductAnalyticalRawData_ProductAnalyt~",
                table: "RouteOperationAction");

            migrationBuilder.RenameColumn(
                name: "ProductAnalyticalRawDataId",
                table: "RouteOperationAction",
                newName: "FormId");

            migrationBuilder.RenameIndex(
                name: "IX_RouteOperationAction_ProductAnalyticalRawDataId",
                table: "RouteOperationAction",
                newName: "IX_RouteOperationAction_FormId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteOperationAction_Forms_FormId",
                table: "RouteOperationAction",
                column: "FormId",
                principalTable: "Forms",
                principalColumn: "Id");
        }
    }
}
