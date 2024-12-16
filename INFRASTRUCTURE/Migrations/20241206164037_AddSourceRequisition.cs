using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceRequisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SourceRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceRequisitions_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SourceRequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceRequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_SourceRequisitions_SourceRequisition~",
                        column: x => x.SourceRequisitionId,
                        principalTable: "SourceRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SourceRequisitionItemSuppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRequisitionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentQuotationRequestAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "IX_SourceRequisitionItems_CreatedById",
                table: "SourceRequisitionItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_LastDeletedById",
                table: "SourceRequisitionItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_LastUpdatedById",
                table: "SourceRequisitionItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_MaterialId",
                table: "SourceRequisitionItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_SourceRequisitionId",
                table: "SourceRequisitionItems",
                column: "SourceRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_UoMId",
                table: "SourceRequisitionItems",
                column: "UoMId");

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

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitions_CreatedById",
                table: "SourceRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitions_LastDeletedById",
                table: "SourceRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitions_LastUpdatedById",
                table: "SourceRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitions_RequisitionId",
                table: "SourceRequisitions",
                column: "RequisitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourceRequisitionItemSuppliers");

            migrationBuilder.DropTable(
                name: "SourceRequisitionItems");

            migrationBuilder.DropTable(
                name: "SourceRequisitions");
        }
    }
}
