using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAssigneeFromForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormFields_users_AssigneeId",
                table: "FormFields");

            migrationBuilder.DropForeignKey(
                name: "FK_FormFields_users_ReviewerId",
                table: "FormFields");

            migrationBuilder.DropForeignKey(
                name: "FK_FormSections_users_AssigneeId",
                table: "FormSections");

            migrationBuilder.DropIndex(
                name: "IX_FormSections_AssigneeId",
                table: "FormSections");

            migrationBuilder.DropIndex(
                name: "IX_FormFields_AssigneeId",
                table: "FormFields");

            migrationBuilder.DropIndex(
                name: "IX_FormFields_ReviewerId",
                table: "FormFields");

            migrationBuilder.DropColumn(
                name: "AssigneeId",
                table: "FormSections");

            migrationBuilder.DropColumn(
                name: "AssigneeId",
                table: "FormFields");

            migrationBuilder.DropColumn(
                name: "ReviewerId",
                table: "FormFields");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssigneeId",
                table: "FormSections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssigneeId",
                table: "FormFields",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewerId",
                table: "FormFields",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_AssigneeId",
                table: "FormSections",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_AssigneeId",
                table: "FormFields",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_ReviewerId",
                table: "FormFields",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormFields_users_AssigneeId",
                table: "FormFields",
                column: "AssigneeId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormFields_users_ReviewerId",
                table: "FormFields",
                column: "ReviewerId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FormSections_users_AssigneeId",
                table: "FormSections",
                column: "AssigneeId",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
