using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffRequisitionApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitionApproval_Approvals_ApprovalId",
                table: "StaffRequisitionApproval");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitionApproval_StaffRequisitions_StaffRequisition~",
                table: "StaffRequisitionApproval");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitionApproval_roles_RoleId",
                table: "StaffRequisitionApproval");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitionApproval_users_ApprovedById",
                table: "StaffRequisitionApproval");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitionApproval_users_UserId",
                table: "StaffRequisitionApproval");
            

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaffRequisitionApproval",
                table: "StaffRequisitionApproval");

            migrationBuilder.RenameTable(
                name: "StaffRequisitionApproval",
                newName: "StaffRequisitionApprovals");

            migrationBuilder.RenameIndex(
                name: "IX_StaffRequisitionApproval_UserId",
                table: "StaffRequisitionApprovals",
                newName: "IX_StaffRequisitionApprovals_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffRequisitionApproval_StaffRequisitionId",
                table: "StaffRequisitionApprovals",
                newName: "IX_StaffRequisitionApprovals_StaffRequisitionId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffRequisitionApproval_RoleId",
                table: "StaffRequisitionApprovals",
                newName: "IX_StaffRequisitionApprovals_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffRequisitionApproval_ApprovedById",
                table: "StaffRequisitionApprovals",
                newName: "IX_StaffRequisitionApprovals_ApprovedById");

            migrationBuilder.RenameIndex(
                name: "IX_StaffRequisitionApproval_ApprovalId",
                table: "StaffRequisitionApprovals",
                newName: "IX_StaffRequisitionApprovals_ApprovalId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaffRequisitionApprovals",
                table: "StaffRequisitionApprovals",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ProductAnalyticalRawData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StpNumber = table.Column<string>(type: "text", nullable: true),
                    SpecNumber = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    StpId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAnalyticalRawData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAnalyticalRawData_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductAnalyticalRawData_ProductStandardTestProcedures_StpId",
                        column: x => x.StpId,
                        principalTable: "ProductStandardTestProcedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductAnalyticalRawData_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductAnalyticalRawData_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductAnalyticalRawData_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAnalyticalRawData_CreatedById",
                table: "MaterialAnalyticalRawData",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAnalyticalRawData_FormId",
                table: "MaterialAnalyticalRawData",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAnalyticalRawData_LastDeletedById",
                table: "MaterialAnalyticalRawData",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAnalyticalRawData_LastUpdatedById",
                table: "MaterialAnalyticalRawData",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAnalyticalRawData_StpId",
                table: "MaterialAnalyticalRawData",
                column: "StpId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAnalyticalRawData_CreatedById",
                table: "ProductAnalyticalRawData",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAnalyticalRawData_FormId",
                table: "ProductAnalyticalRawData",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAnalyticalRawData_LastDeletedById",
                table: "ProductAnalyticalRawData",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAnalyticalRawData_LastUpdatedById",
                table: "ProductAnalyticalRawData",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAnalyticalRawData_StpId",
                table: "ProductAnalyticalRawData",
                column: "StpId");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitionApprovals_Approvals_ApprovalId",
                table: "StaffRequisitionApprovals",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitionApprovals_StaffRequisitions_StaffRequisitio~",
                table: "StaffRequisitionApprovals",
                column: "StaffRequisitionId",
                principalTable: "StaffRequisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitionApprovals_roles_RoleId",
                table: "StaffRequisitionApprovals",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitionApprovals_users_ApprovedById",
                table: "StaffRequisitionApprovals",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitionApprovals_users_UserId",
                table: "StaffRequisitionApprovals",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitionApprovals_Approvals_ApprovalId",
                table: "StaffRequisitionApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitionApprovals_StaffRequisitions_StaffRequisitio~",
                table: "StaffRequisitionApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitionApprovals_roles_RoleId",
                table: "StaffRequisitionApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitionApprovals_users_ApprovedById",
                table: "StaffRequisitionApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitionApprovals_users_UserId",
                table: "StaffRequisitionApprovals");

            migrationBuilder.DropTable(
                name: "MaterialAnalyticalRawData");

            migrationBuilder.DropTable(
                name: "ProductAnalyticalRawData");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaffRequisitionApprovals",
                table: "StaffRequisitionApprovals");

            migrationBuilder.RenameTable(
                name: "StaffRequisitionApprovals",
                newName: "StaffRequisitionApproval");

            migrationBuilder.RenameIndex(
                name: "IX_StaffRequisitionApprovals_UserId",
                table: "StaffRequisitionApproval",
                newName: "IX_StaffRequisitionApproval_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffRequisitionApprovals_StaffRequisitionId",
                table: "StaffRequisitionApproval",
                newName: "IX_StaffRequisitionApproval_StaffRequisitionId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffRequisitionApprovals_RoleId",
                table: "StaffRequisitionApproval",
                newName: "IX_StaffRequisitionApproval_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffRequisitionApprovals_ApprovedById",
                table: "StaffRequisitionApproval",
                newName: "IX_StaffRequisitionApproval_ApprovedById");

            migrationBuilder.RenameIndex(
                name: "IX_StaffRequisitionApprovals_ApprovalId",
                table: "StaffRequisitionApproval",
                newName: "IX_StaffRequisitionApproval_ApprovalId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaffRequisitionApproval",
                table: "StaffRequisitionApproval",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitionApproval_Approvals_ApprovalId",
                table: "StaffRequisitionApproval",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitionApproval_StaffRequisitions_StaffRequisition~",
                table: "StaffRequisitionApproval",
                column: "StaffRequisitionId",
                principalTable: "StaffRequisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitionApproval_roles_RoleId",
                table: "StaffRequisitionApproval",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitionApproval_users_ApprovedById",
                table: "StaffRequisitionApproval",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitionApproval_users_UserId",
                table: "StaffRequisitionApproval",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
