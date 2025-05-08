using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_PurchaseOrder_Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmountInFigures",
                table: "PurchaseOrders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryModeId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedDeliveryDate",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProFormaInvoiceNumber",
                table: "PurchaseOrders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SeaFreight",
                table: "PurchaseOrders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "TermsOfPaymentId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCifValue",
                table: "PurchaseOrders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalFobValue",
                table: "PurchaseOrders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "DeliveryModes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryModes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryModes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryModes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryModes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TermsOfPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermsOfPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TermsOfPayments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TermsOfPayments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TermsOfPayments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_DeliveryModeId",
                table: "PurchaseOrders",
                column: "DeliveryModeId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TermsOfPaymentId",
                table: "PurchaseOrders",
                column: "TermsOfPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryModes_CreatedById",
                table: "DeliveryModes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryModes_LastDeletedById",
                table: "DeliveryModes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryModes_LastUpdatedById",
                table: "DeliveryModes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TermsOfPayments_CreatedById",
                table: "TermsOfPayments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TermsOfPayments_LastDeletedById",
                table: "TermsOfPayments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TermsOfPayments_LastUpdatedById",
                table: "TermsOfPayments",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_DeliveryModes_DeliveryModeId",
                table: "PurchaseOrders",
                column: "DeliveryModeId",
                principalTable: "DeliveryModes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_TermsOfPayments_TermsOfPaymentId",
                table: "PurchaseOrders",
                column: "TermsOfPaymentId",
                principalTable: "TermsOfPayments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_DeliveryModes_DeliveryModeId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_TermsOfPayments_TermsOfPaymentId",
                table: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "DeliveryModes");

            migrationBuilder.DropTable(
                name: "TermsOfPayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_DeliveryModeId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TermsOfPaymentId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "AmountInFigures",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryModeId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryDate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ProFormaInvoiceNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SeaFreight",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TermsOfPaymentId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TotalCifValue",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TotalFobValue",
                table: "PurchaseOrders");
        }
    }
}
