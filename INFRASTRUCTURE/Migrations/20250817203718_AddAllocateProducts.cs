using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocateProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllocateProductionOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fulfilled = table.Column<bool>(type: "boolean", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocateProductionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrders_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrders_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrders_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrders_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AllocateProductionOrderApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocateProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    StageStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocateProductionOrderApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrderApprovals_AllocateProductionOrders_A~",
                        column: x => x.AllocateProductionOrderId,
                        principalTable: "AllocateProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrderApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrderApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrderApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrderApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AllocateProductionOrderProduct",
                columns: table => new
                {
                    AllocateProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocateProductionOrderProduct", x => new { x.AllocateProductionOrderId, x.Id });
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrderProduct_AllocateProductionOrders_All~",
                        column: x => x.AllocateProductionOrderId,
                        principalTable: "AllocateProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllocateProductionOrderProduct_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AllocateProductQuantity",
                columns: table => new
                {
                    AllocateProductionOrderProductAllocateProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocateProductionOrderProductId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinishedGoodsTransferNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocateProductQuantity", x => new { x.AllocateProductionOrderProductAllocateProductionOrderId, x.AllocateProductionOrderProductId, x.Id });
                    table.ForeignKey(
                        name: "FK_AllocateProductQuantity_AllocateProductionOrderProduct_Allo~",
                        columns: x => new { x.AllocateProductionOrderProductAllocateProductionOrderId, x.AllocateProductionOrderProductId },
                        principalTable: "AllocateProductionOrderProduct",
                        principalColumns: new[] { "AllocateProductionOrderId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllocateProductQuantity_FinishedGoodsTransferNotes_Finished~",
                        column: x => x.FinishedGoodsTransferNoteId,
                        principalTable: "FinishedGoodsTransferNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrderApprovals_AllocateProductionOrderId",
                table: "AllocateProductionOrderApprovals",
                column: "AllocateProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrderApprovals_ApprovalId",
                table: "AllocateProductionOrderApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrderApprovals_ApprovedById",
                table: "AllocateProductionOrderApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrderApprovals_RoleId",
                table: "AllocateProductionOrderApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrderApprovals_UserId",
                table: "AllocateProductionOrderApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrderProduct_ProductId",
                table: "AllocateProductionOrderProduct",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrders_CreatedById",
                table: "AllocateProductionOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrders_LastDeletedById",
                table: "AllocateProductionOrders",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrders_LastUpdatedById",
                table: "AllocateProductionOrders",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductionOrders_ProductionOrderId",
                table: "AllocateProductionOrders",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocateProductQuantity_FinishedGoodsTransferNoteId",
                table: "AllocateProductQuantity",
                column: "FinishedGoodsTransferNoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllocateProductionOrderApprovals");

            migrationBuilder.DropTable(
                name: "AllocateProductQuantity");

            migrationBuilder.DropTable(
                name: "AllocateProductionOrderProduct");

            migrationBuilder.DropTable(
                name: "AllocateProductionOrders");
        }
    }
}
