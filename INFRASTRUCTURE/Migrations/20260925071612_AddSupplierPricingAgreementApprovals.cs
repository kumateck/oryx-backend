using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierPricingAgreementApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChangeKind",
                table: "SupplierPricingAgreements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacesAgreementId",
                table: "SupplierPricingAgreements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "SupplierPricingAgreements",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "SupplierPricingAgreementApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPricingAgreementId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SupplierPricingAgreementApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreementApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreementApprovals_SupplierPricingAgreements~",
                        column: x => x.SupplierPricingAgreementId,
                        principalTable: "SupplierPricingAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreementApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreementApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierPricingAgreementApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreementApprovals_ApprovalId",
                table: "SupplierPricingAgreementApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreementApprovals_ApprovedById",
                table: "SupplierPricingAgreementApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreementApprovals_RoleId",
                table: "SupplierPricingAgreementApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreementApprovals_SupplierPricingAgreementId",
                table: "SupplierPricingAgreementApprovals",
                column: "SupplierPricingAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPricingAgreementApprovals_UserId",
                table: "SupplierPricingAgreementApprovals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierPricingAgreementApprovals");

            migrationBuilder.DropColumn(
                name: "ChangeKind",
                table: "SupplierPricingAgreements");

            migrationBuilder.DropColumn(
                name: "ReplacesAgreementId",
                table: "SupplierPricingAgreements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SupplierPricingAgreements");
        }
    }
}
