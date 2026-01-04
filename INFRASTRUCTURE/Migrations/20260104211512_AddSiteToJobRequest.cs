using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteToJobRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "JobRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "SiteId",
                table: "JobRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobRequests_SiteId",
                table: "JobRequests",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobRequests_Sites_SiteId",
                table: "JobRequests",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobRequests_Sites_SiteId",
                table: "JobRequests");

            migrationBuilder.DropIndex(
                name: "IX_JobRequests_SiteId",
                table: "JobRequests");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "JobRequests");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "JobRequests",
                type: "text",
                nullable: true);
        }
    }
}
