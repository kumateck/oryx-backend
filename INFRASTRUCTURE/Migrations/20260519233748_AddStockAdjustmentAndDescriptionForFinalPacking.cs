using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStockAdjustmentAndDescriptionForFinalPacking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "StockAdjustments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PercentageVarianceDescription",
                table: "FinalPackings",
                type: "character varying(10000000)",
                maxLength: 10000000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PercentageVarianceDescription",
                table: "FinalPackingMaterials",
                type: "character varying(10000000)",
                maxLength: 10000000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockAdjustmentApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockAdjustmentId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_StockAdjustmentApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentApprovals_StockAdjustments_StockAdjustmentId",
                        column: x => x.StockAdjustmentId,
                        principalTable: "StockAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustmentApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockAdjustmentApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentApprovals_ApprovalId",
                table: "StockAdjustmentApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentApprovals_ApprovedById",
                table: "StockAdjustmentApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentApprovals_RoleId",
                table: "StockAdjustmentApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentApprovals_StockAdjustmentId",
                table: "StockAdjustmentApprovals",
                column: "StockAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentApprovals_UserId",
                table: "StockAdjustmentApprovals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockAdjustmentApprovals");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "PercentageVarianceDescription",
                table: "FinalPackings");

            migrationBuilder.DropColumn(
                name: "PercentageVarianceDescription",
                table: "FinalPackingMaterials");
        }
    }
}
