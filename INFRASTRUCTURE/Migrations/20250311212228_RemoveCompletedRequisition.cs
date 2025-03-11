using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompletedRequisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompletedRequisitionItems");

            migrationBuilder.DropTable(
                name: "CompletedRequisitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompletedRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequisitionType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CompletedRequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedRequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_CompletedRequisitions_CompletedRe~",
                        column: x => x.CompletedRequisitionId,
                        principalTable: "CompletedRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_CompletedRequisitionId",
                table: "CompletedRequisitionItems",
                column: "CompletedRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_CreatedById",
                table: "CompletedRequisitionItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_LastDeletedById",
                table: "CompletedRequisitionItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_LastUpdatedById",
                table: "CompletedRequisitionItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_MaterialId",
                table: "CompletedRequisitionItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_UoMId",
                table: "CompletedRequisitionItems",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_CreatedById",
                table: "CompletedRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_LastDeletedById",
                table: "CompletedRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_LastUpdatedById",
                table: "CompletedRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_RequisitionId",
                table: "CompletedRequisitions",
                column: "RequisitionId");
        }
    }
}
