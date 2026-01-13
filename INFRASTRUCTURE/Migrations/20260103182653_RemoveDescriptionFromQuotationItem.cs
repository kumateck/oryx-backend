using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDescriptionFromQuotationItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ServiceProformaInvoiceItems");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "ServiceProformaInvoiceItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "QuotationItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ServiceProformaInvoiceItems",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "ServiceProformaInvoiceItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "QuotationItems",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
