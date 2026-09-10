using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddRndAnalyticalMethodModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RndAnalyticalMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    MethodName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ValidationProtocolFormId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndAnalyticalMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndAnalyticalMethods_Forms_ValidationProtocolFormId",
                        column: x => x.ValidationProtocolFormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndAnalyticalMethods_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndAnalyticalMethods_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RndAnalyticalMethods_RndProjects_RndProjectId",
                        column: x => x.RndProjectId,
                        principalTable: "RndProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RndAnalyticalMethods_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndAnalyticalMethods_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndAnalyticalMethods_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RndAnalyticalMethods_users_ValidatedById",
                        column: x => x.ValidatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RndAnalyticalMethods_CreatedById",
                table: "RndAnalyticalMethods",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndAnalyticalMethods_LastDeletedById",
                table: "RndAnalyticalMethods",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndAnalyticalMethods_LastUpdatedById",
                table: "RndAnalyticalMethods",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndAnalyticalMethods_MaterialId",
                table: "RndAnalyticalMethods",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_RndAnalyticalMethods_ProductId",
                table: "RndAnalyticalMethods",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RndAnalyticalMethods_RndProjectId",
                table: "RndAnalyticalMethods",
                column: "RndProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RndAnalyticalMethods_ValidatedById",
                table: "RndAnalyticalMethods",
                column: "ValidatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RndAnalyticalMethods_ValidationProtocolFormId",
                table: "RndAnalyticalMethods",
                column: "ValidationProtocolFormId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RndAnalyticalMethods");
        }
    }
}
