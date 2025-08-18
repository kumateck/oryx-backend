using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceProviderId",
                table: "Services",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProviders_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceProviders_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceProviders_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceProviders_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceProviders_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Services_ServiceProviderId",
                table: "Services",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_CountryId",
                table: "ServiceProviders",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_CreatedById",
                table: "ServiceProviders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_CurrencyId",
                table: "ServiceProviders",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_LastDeletedById",
                table: "ServiceProviders",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_LastUpdatedById",
                table: "ServiceProviders",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_ServiceProviders_ServiceProviderId",
                table: "Services",
                column: "ServiceProviderId",
                principalTable: "ServiceProviders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_ServiceProviders_ServiceProviderId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "ServiceProviders");

            migrationBuilder.DropIndex(
                name: "IX_Services_ServiceProviderId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ServiceProviderId",
                table: "Services");
        }
    }
}
