using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCompletedRequisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompletedRequisitions_users_RequestedById",
                table: "CompletedRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_CompletedRequisitions_RequestedById",
                table: "CompletedRequisitions");

            migrationBuilder.DropColumn(
                name: "RequestedById",
                table: "CompletedRequisitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestedById",
                table: "CompletedRequisitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_RequestedById",
                table: "CompletedRequisitions",
                column: "RequestedById");

            migrationBuilder.AddForeignKey(
                name: "FK_CompletedRequisitions_users_RequestedById",
                table: "CompletedRequisitions",
                column: "RequestedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
