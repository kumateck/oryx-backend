using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ServiceAndServiceProviderRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_ServiceProviders_ServiceProviderId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_ServiceProviderId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ServiceProviderId",
                table: "Services");

            migrationBuilder.CreateTable(
                name: "ServiceServiceProvider",
                columns: table => new
                {
                    ServiceProvidersId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceServiceProvider", x => new { x.ServiceProvidersId, x.ServicesId });
                    table.ForeignKey(
                        name: "FK_ServiceServiceProvider_ServiceProviders_ServiceProvidersId",
                        column: x => x.ServiceProvidersId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceServiceProvider_Services_ServicesId",
                        column: x => x.ServicesId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceServiceProvider_ServicesId",
                table: "ServiceServiceProvider",
                column: "ServicesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceServiceProvider");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceProviderId",
                table: "Services",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_ServiceProviderId",
                table: "Services",
                column: "ServiceProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_ServiceProviders_ServiceProviderId",
                table: "Services",
                column: "ServiceProviderId",
                principalTable: "ServiceProviders",
                principalColumn: "Id");
        }
    }
}
