using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialSpecToFormSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssigneeId",
                table: "FormSections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialSpecificationId",
                table: "FormSections",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_AssigneeId",
                table: "FormSections",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_MaterialSpecificationId",
                table: "FormSections",
                column: "MaterialSpecificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormSections_MaterialSpecifications_MaterialSpecificationId",
                table: "FormSections",
                column: "MaterialSpecificationId",
                principalTable: "MaterialSpecifications",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormSections_users_AssigneeId",
                table: "FormSections",
                column: "AssigneeId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormSections_MaterialSpecifications_MaterialSpecificationId",
                table: "FormSections");

            migrationBuilder.DropForeignKey(
                name: "FK_FormSections_users_AssigneeId",
                table: "FormSections");

            migrationBuilder.DropIndex(
                name: "IX_FormSections_AssigneeId",
                table: "FormSections");

            migrationBuilder.DropIndex(
                name: "IX_FormSections_MaterialSpecificationId",
                table: "FormSections");

            migrationBuilder.DropColumn(
                name: "AssigneeId",
                table: "FormSections");

            migrationBuilder.DropColumn(
                name: "MaterialSpecificationId",
                table: "FormSections");
        }
    }
}
