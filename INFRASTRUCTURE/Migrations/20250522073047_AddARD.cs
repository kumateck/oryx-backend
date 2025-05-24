using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddARD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "MaterialAnalyticalRawData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StpNumber = table.Column<string>(type: "text", nullable: true),
                    SpecNumber = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticalRawData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticalRawData_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalyticalRawData_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalyticalRawData_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnalyticalRawData_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnalyticalRawData_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_CreatedById",
                table: "MaterialAnalyticalRawData",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_FormId",
                table: "MaterialAnalyticalRawData",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_LastDeletedById",
                table: "MaterialAnalyticalRawData",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_LastUpdatedById",
                table: "MaterialAnalyticalRawData",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRawData_MaterialId",
                table: "MaterialAnalyticalRawData",
                column: "MaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialAnalyticalRawData");
            
        }
    }
}
