using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddRndCentralApprovalWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "RndTechnologyTransfers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "RndFormulations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RndFormulationApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndFormulationId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_RndFormulationApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndFormulationApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndFormulationApprovals_RndFormulations_RndFormulationId",
                        column: x => x.RndFormulationId,
                        principalTable: "RndFormulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndFormulationApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndFormulationApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndFormulationApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RndTechnologyTransferApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndTechnologyTransferId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_RndTechnologyTransferApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransferApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransferApprovals_RndTechnologyTransfers_RndTe~",
                        column: x => x.RndTechnologyTransferId,
                        principalTable: "RndTechnologyTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransferApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransferApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndTechnologyTransferApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationApprovals_ApprovalId_RndFormulationId_Order_U~",
                table: "RndFormulationApprovals",
                columns: new[] { "ApprovalId", "RndFormulationId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationApprovals_ApprovedById",
                table: "RndFormulationApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationApprovals_RndFormulationId",
                table: "RndFormulationApprovals",
                column: "RndFormulationId");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationApprovals_RoleId",
                table: "RndFormulationApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RndFormulationApprovals_UserId",
                table: "RndFormulationApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransferApprovals_ApprovalId_RndTechnologyTran~",
                table: "RndTechnologyTransferApprovals",
                columns: new[] { "ApprovalId", "RndTechnologyTransferId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransferApprovals_ApprovedById",
                table: "RndTechnologyTransferApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransferApprovals_RndTechnologyTransferId",
                table: "RndTechnologyTransferApprovals",
                column: "RndTechnologyTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransferApprovals_RoleId",
                table: "RndTechnologyTransferApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RndTechnologyTransferApprovals_UserId",
                table: "RndTechnologyTransferApprovals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RndFormulationApprovals");

            migrationBuilder.DropTable(
                name: "RndTechnologyTransferApprovals");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "RndTechnologyTransfers");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "RndFormulations");
        }
    }
}
