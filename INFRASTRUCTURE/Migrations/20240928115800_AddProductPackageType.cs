using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPackageType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductPackageTypeId",
                table: "ProductPackages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductPackageTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPackageTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPackageTypes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductPackageTypes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductPackageTypes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_ProductPackageTypeId",
                table: "ProductPackages",
                column: "ProductPackageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackageTypes_CreatedById",
                table: "ProductPackageTypes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackageTypes_LastDeletedById",
                table: "ProductPackageTypes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackageTypes_LastUpdatedById",
                table: "ProductPackageTypes",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_ProductPackageTypes_ProductPackageTypeId",
                table: "ProductPackages",
                column: "ProductPackageTypeId",
                principalTable: "ProductPackageTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_ProductPackageTypes_ProductPackageTypeId",
                table: "ProductPackages");

            migrationBuilder.DropTable(
                name: "ProductPackageTypes");

            migrationBuilder.DropIndex(
                name: "IX_ProductPackages_ProductPackageTypeId",
                table: "ProductPackages");

            migrationBuilder.DropColumn(
                name: "ProductPackageTypeId",
                table: "ProductPackages");
        }
    }
}
