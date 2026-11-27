using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialCoaReportedTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ProductSpecificationId",
                table: "SpecificationCoaReportings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialSpecificationId",
                table: "SpecificationCoaReportings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationCoaReportings_MaterialSpecificationId_FormFiel~",
                table: "SpecificationCoaReportings",
                columns: new[] { "MaterialSpecificationId", "FormFieldId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SpecificationCoaReportings_OneSpecification",
                table: "SpecificationCoaReportings",
                sql: "(\"MaterialSpecificationId\" IS NULL) <> (\"ProductSpecificationId\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_SpecificationCoaReportings_MaterialSpecifications_MaterialS~",
                table: "SpecificationCoaReportings",
                column: "MaterialSpecificationId",
                principalTable: "MaterialSpecifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Material overrides have no product specification to fall back to.
            migrationBuilder.Sql("DELETE FROM \"SpecificationCoaReportings\" WHERE \"ProductSpecificationId\" IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_SpecificationCoaReportings_MaterialSpecifications_MaterialS~",
                table: "SpecificationCoaReportings");

            migrationBuilder.DropIndex(
                name: "IX_SpecificationCoaReportings_MaterialSpecificationId_FormFiel~",
                table: "SpecificationCoaReportings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SpecificationCoaReportings_OneSpecification",
                table: "SpecificationCoaReportings");

            migrationBuilder.DropColumn(
                name: "MaterialSpecificationId",
                table: "SpecificationCoaReportings");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductSpecificationId",
                table: "SpecificationCoaReportings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
