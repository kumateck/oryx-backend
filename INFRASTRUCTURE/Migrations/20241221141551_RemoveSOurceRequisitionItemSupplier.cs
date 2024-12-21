using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSOurceRequisitionItemSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourceRequisitionItemSuppliers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SourceRequisitionItemSuppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceRequisitionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceRequisitionItemSuppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItemSuppliers_SourceRequisitionItems_Sourc~",
                        column: x => x.SourceRequisitionItemId,
                        principalTable: "SourceRequisitionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItemSuppliers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItemSuppliers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItemSuppliers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItemSuppliers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItemSuppliers_CreatedById",
                table: "SourceRequisitionItemSuppliers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItemSuppliers_LastDeletedById",
                table: "SourceRequisitionItemSuppliers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItemSuppliers_LastUpdatedById",
                table: "SourceRequisitionItemSuppliers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItemSuppliers_SourceRequisitionItemId",
                table: "SourceRequisitionItemSuppliers",
                column: "SourceRequisitionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItemSuppliers_SupplierId",
                table: "SourceRequisitionItemSuppliers",
                column: "SupplierId");
        }
    }
}
