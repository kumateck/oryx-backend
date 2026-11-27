using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddCoaReportedTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReportOnCoa",
                table: "FormFields",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "SpecificationCoaReportings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductSpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportOnCoa = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SavedById = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationCoaReportings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificationCoaReportings_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpecificationCoaReportings_ProductSpecifications_ProductSpe~",
                        column: x => x.ProductSpecificationId,
                        principalTable: "ProductSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpecificationCoaReportings_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SpecificationCoaReportings_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SpecificationCoaReportings_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationCoaReportings_CreatedById",
                table: "SpecificationCoaReportings",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationCoaReportings_FormFieldId",
                table: "SpecificationCoaReportings",
                column: "FormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationCoaReportings_LastDeletedById",
                table: "SpecificationCoaReportings",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationCoaReportings_LastUpdatedById",
                table: "SpecificationCoaReportings",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationCoaReportings_ProductSpecificationId_FormField~",
                table: "SpecificationCoaReportings",
                columns: new[] { "ProductSpecificationId", "FormFieldId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecificationCoaReportings");

            migrationBuilder.DropColumn(
                name: "ReportOnCoa",
                table: "FormFields");
        }
    }
}
