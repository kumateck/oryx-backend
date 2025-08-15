using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityReceivedFinishedGoodsTransferNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "Items");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "FinishedGoodsTransferNotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityReceived",
                table: "FinishedGoodsTransferNotes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.DropColumn(
                name: "QuantityReceived",
                table: "FinishedGoodsTransferNotes");

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "Items",
                type: "text",
                nullable: true);
        }
    }
}
