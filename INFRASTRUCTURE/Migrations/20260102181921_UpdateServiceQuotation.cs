using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServiceQuotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceCharge",
                table: "ServiceQuotations");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "ServiceProformaInvoiceItems");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "QuotationItems");

            migrationBuilder.CreateTable(
                name: "ServiceCharge",
                columns: table => new
                {
                    ServiceQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Cost = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCharge", x => new { x.ServiceQuotationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ServiceCharge_ServiceQuotations_ServiceQuotationId",
                        column: x => x.ServiceQuotationId,
                        principalTable: "ServiceQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceCharge");

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceCharge",
                table: "ServiceQuotations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "ServiceProformaInvoiceItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "QuotationItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
