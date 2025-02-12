using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Addded_QuantityReceived_RequisitionItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Distributed",
                table: "ShipmentInvoicesItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityReceived",
                table: "RequisitionItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Distributed",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropColumn(
                name: "QuantityReceived",
                table: "RequisitionItems");
        }
    }
}
