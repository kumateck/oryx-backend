using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStockIssueBinCardReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductBatchNumber",
                table: "BinCardInformation",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequisitionCode",
                table: "BinCardInformation",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequisitionId",
                table: "BinCardInformation",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductBatchNumber",
                table: "BinCardInformation");

            migrationBuilder.DropColumn(
                name: "RequisitionCode",
                table: "BinCardInformation");

            migrationBuilder.DropColumn(
                name: "RequisitionId",
                table: "BinCardInformation");
        }
    }
}
