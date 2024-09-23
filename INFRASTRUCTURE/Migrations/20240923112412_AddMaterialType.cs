using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_MaterialTypes_MaterialTypeId",
                table: "Materials");

            migrationBuilder.RenameColumn(
                name: "MaterialTypeId",
                table: "Materials",
                newName: "MaterialCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_MaterialTypeId",
                table: "Materials",
                newName: "IX_Materials_MaterialCategoryId");

            migrationBuilder.CreateTable(
                name: "MaterialCategories",
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
                    table.PrimaryKey("PK_MaterialCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialCategories_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialCategories_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialCategories_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCategories_CreatedById",
                table: "MaterialCategories",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCategories_LastDeletedById",
                table: "MaterialCategories",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCategories_LastUpdatedById",
                table: "MaterialCategories",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_MaterialCategories_MaterialCategoryId",
                table: "Materials",
                column: "MaterialCategoryId",
                principalTable: "MaterialCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_MaterialCategories_MaterialCategoryId",
                table: "Materials");

            migrationBuilder.DropTable(
                name: "MaterialCategories");

            migrationBuilder.RenameColumn(
                name: "MaterialCategoryId",
                table: "Materials",
                newName: "MaterialTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_MaterialCategoryId",
                table: "Materials",
                newName: "IX_Materials_MaterialTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_MaterialTypes_MaterialTypeId",
                table: "Materials",
                column: "MaterialTypeId",
                principalTable: "MaterialTypes",
                principalColumn: "Id");
        }
    }
}
