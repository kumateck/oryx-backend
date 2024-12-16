using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRequisitionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Processed",
                table: "SourceRequisitionItemSuppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedQuotationAt",
                table: "SourceRequisitionItemSuppliers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SupplierQuotedPrice",
                table: "SourceRequisitionItemSuppliers",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Processed",
                table: "SourceRequisitionItemSuppliers");

            migrationBuilder.DropColumn(
                name: "ReceivedQuotationAt",
                table: "SourceRequisitionItemSuppliers");

            migrationBuilder.DropColumn(
                name: "SupplierQuotedPrice",
                table: "SourceRequisitionItemSuppliers");
        }
    }
}
