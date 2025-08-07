using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAtrAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "AnalyticalTestRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcknowledgedById",
                table: "AnalyticalTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemStockRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequisitionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Justification = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemStockRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitions_users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalTestRequests_AcknowledgedById",
                table: "AnalyticalTestRequests",
                column: "AcknowledgedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitions_CreatedById",
                table: "ItemStockRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitions_DepartmentId",
                table: "ItemStockRequisitions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitions_LastDeletedById",
                table: "ItemStockRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitions_LastUpdatedById",
                table: "ItemStockRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitions_RequestedById",
                table: "ItemStockRequisitions",
                column: "RequestedById");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequests_users_AcknowledgedById",
                table: "AnalyticalTestRequests",
                column: "AcknowledgedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequests_users_AcknowledgedById",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropTable(
                name: "ItemStockRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticalTestRequests_AcknowledgedById",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "AnalyticalTestRequests");

            migrationBuilder.DropColumn(
                name: "AcknowledgedById",
                table: "AnalyticalTestRequests");
        }
    }
}
