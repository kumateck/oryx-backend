using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentDocumentApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "ShipmentDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ShipmentDocumentApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ShipmentDocumentApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDocumentApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDocumentApprovals_ShipmentDocuments_ShipmentDocumen~",
                        column: x => x.ShipmentDocumentId,
                        principalTable: "ShipmentDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDocumentApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDocumentApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDocumentApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocumentApprovals_ApprovalId",
                table: "ShipmentDocumentApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocumentApprovals_ApprovedById",
                table: "ShipmentDocumentApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocumentApprovals_RoleId",
                table: "ShipmentDocumentApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocumentApprovals_ShipmentDocumentId",
                table: "ShipmentDocumentApprovals",
                column: "ShipmentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocumentApprovals_UserId",
                table: "ShipmentDocumentApprovals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentDocumentApprovals");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "ShipmentDocuments");
        }
    }
}
