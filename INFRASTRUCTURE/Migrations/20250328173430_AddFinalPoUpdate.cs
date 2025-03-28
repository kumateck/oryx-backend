using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalPoUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionItems_UnitOfMeasures_UomId",
                table: "RequisitionItems");

            migrationBuilder.RenameColumn(
                name: "UomId",
                table: "RequisitionItems",
                newName: "UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_RequisitionItems_UomId",
                table: "RequisitionItems",
                newName: "IX_RequisitionItems_UoMId");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "RequisitionItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionItems_UnitOfMeasures_UoMId",
                table: "RequisitionItems",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionItems_UnitOfMeasures_UoMId",
                table: "RequisitionItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "RequisitionItems");

            migrationBuilder.RenameColumn(
                name: "UoMId",
                table: "RequisitionItems",
                newName: "UomId");

            migrationBuilder.RenameIndex(
                name: "IX_RequisitionItems_UoMId",
                table: "RequisitionItems",
                newName: "IX_RequisitionItems_UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionItems_UnitOfMeasures_UomId",
                table: "RequisitionItems",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }
    }
}
