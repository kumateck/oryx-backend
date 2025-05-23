using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffRequisitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaffRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffRequired = table.Column<int>(type: "integer", nullable: false),
                    BudgetStatus = table.Column<int>(type: "integer", nullable: false),
                    AppointmentType = table.Column<int>(type: "integer", nullable: false),
                    StaffRequisitionStatus = table.Column<int>(type: "integer", nullable: false),
                    RequestUrgency = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Justification = table.Column<string>(type: "text", nullable: true),
                    Qualification = table.Column<string>(type: "text", nullable: true),
                    EducationalQualification = table.Column<string>(type: "text", nullable: true),
                    AdditionalRequirements = table.Column<string>(type: "text", nullable: true),
                    DesignationId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_StaffRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffRequisitions_Designations_DesignationId",
                        column: x => x.DesignationId,
                        principalTable: "Designations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StaffRequisitionApproval",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_StaffRequisitionApproval", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffRequisitionApproval_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffRequisitionApproval_StaffRequisitions_StaffRequisition~",
                        column: x => x.StaffRequisitionId,
                        principalTable: "StaffRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffRequisitionApproval_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffRequisitionApproval_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffRequisitionApproval_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffRequisitionApproval_ApprovalId",
                table: "StaffRequisitionApproval",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRequisitionApproval_ApprovedById",
                table: "StaffRequisitionApproval",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRequisitionApproval_RoleId",
                table: "StaffRequisitionApproval",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRequisitionApproval_StaffRequisitionId",
                table: "StaffRequisitionApproval",
                column: "StaffRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRequisitionApproval_UserId",
                table: "StaffRequisitionApproval",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRequisitions_CreatedById",
                table: "StaffRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRequisitions_DesignationId",
                table: "StaffRequisitions",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRequisitions_LastDeletedById",
                table: "StaffRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRequisitions_LastUpdatedById",
                table: "StaffRequisitions",
                column: "LastUpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffRequisitionApproval");

            migrationBuilder.DropTable(
                name: "StaffRequisitions");
        }
    }
}
