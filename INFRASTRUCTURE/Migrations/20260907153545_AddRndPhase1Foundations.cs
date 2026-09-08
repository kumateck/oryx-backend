using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddRndPhase1Foundations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CalibrationCertificateAttachmentId",
                table: "QcEquipments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalibrationDueDate",
                table: "QcEquipments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCalibratedAt",
                table: "QcEquipments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualificationStatus",
                table: "QcEquipments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CalibrationCertificateAttachmentId",
                table: "Instruments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalibrationDueDate",
                table: "Instruments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCalibratedAt",
                table: "Instruments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualificationStatus",
                table: "Instruments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RndProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Objective = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TargetLaunchDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QtppFormId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_RndProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndProjects_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndProjects_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndProjects_Forms_QtppFormId",
                        column: x => x.QtppFormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndProjects_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndProjects_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndProjects_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndProjects_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndProjects_users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RndProjectApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_RndProjectApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndProjectApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndProjectApprovals_RndProjects_RndProjectId",
                        column: x => x.RndProjectId,
                        principalTable: "RndProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndProjectApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndProjectApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndProjectApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_QcEquipments_CalibrationCertificateAttachmentId",
                table: "QcEquipments",
                column: "CalibrationCertificateAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_CalibrationCertificateAttachmentId",
                table: "Instruments",
                column: "CalibrationCertificateAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjectApprovals_ApprovalId_RndProjectId_Order_UserId_Ro~",
                table: "RndProjectApprovals",
                columns: new[] { "ApprovalId", "RndProjectId", "Order", "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RndProjectApprovals_ApprovedById",
                table: "RndProjectApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjectApprovals_RndProjectId",
                table: "RndProjectApprovals",
                column: "RndProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjectApprovals_RoleId",
                table: "RndProjectApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjectApprovals_UserId",
                table: "RndProjectApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjects_AttachmentId",
                table: "RndProjects",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjects_CreatedById",
                table: "RndProjects",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjects_DepartmentId",
                table: "RndProjects",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjects_LastDeletedById",
                table: "RndProjects",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjects_LastUpdatedById",
                table: "RndProjects",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjects_ProductId",
                table: "RndProjects",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjects_QtppFormId",
                table: "RndProjects",
                column: "QtppFormId");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjects_RequestedById",
                table: "RndProjects",
                column: "RequestedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Instruments_Attachments_CalibrationCertificateAttachmentId",
                table: "Instruments",
                column: "CalibrationCertificateAttachmentId",
                principalTable: "Attachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QcEquipments_Attachments_CalibrationCertificateAttachmentId",
                table: "QcEquipments",
                column: "CalibrationCertificateAttachmentId",
                principalTable: "Attachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instruments_Attachments_CalibrationCertificateAttachmentId",
                table: "Instruments");

            migrationBuilder.DropForeignKey(
                name: "FK_QcEquipments_Attachments_CalibrationCertificateAttachmentId",
                table: "QcEquipments");

            migrationBuilder.DropTable(
                name: "RndProjectApprovals");

            migrationBuilder.DropTable(
                name: "RndProjects");

            migrationBuilder.DropIndex(
                name: "IX_QcEquipments_CalibrationCertificateAttachmentId",
                table: "QcEquipments");

            migrationBuilder.DropIndex(
                name: "IX_Instruments_CalibrationCertificateAttachmentId",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "CalibrationCertificateAttachmentId",
                table: "QcEquipments");

            migrationBuilder.DropColumn(
                name: "CalibrationDueDate",
                table: "QcEquipments");

            migrationBuilder.DropColumn(
                name: "LastCalibratedAt",
                table: "QcEquipments");

            migrationBuilder.DropColumn(
                name: "QualificationStatus",
                table: "QcEquipments");

            migrationBuilder.DropColumn(
                name: "CalibrationCertificateAttachmentId",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "CalibrationDueDate",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "LastCalibratedAt",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "QualificationStatus",
                table: "Instruments");
        }
    }
}
