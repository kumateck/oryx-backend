using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RenameSTPtoMSTP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StandardTestProcedures");

            migrationBuilder.CreateTable(
                name: "MaterialStandardTestProcedures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StpNumber = table.Column<string>(type: "text", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialStandardTestProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialStandardTestProcedures_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialStandardTestProcedures_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialStandardTestProcedures_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialStandardTestProcedures_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialStandardTestProcedures_CreatedById",
                table: "MaterialStandardTestProcedures",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialStandardTestProcedures_LastDeletedById",
                table: "MaterialStandardTestProcedures",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialStandardTestProcedures_LastUpdatedById",
                table: "MaterialStandardTestProcedures",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialStandardTestProcedures_MaterialId",
                table: "MaterialStandardTestProcedures",
                column: "MaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialStandardTestProcedures");

            migrationBuilder.CreateTable(
                name: "StandardTestProcedures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StpNumber = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardTestProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandardTestProcedures_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandardTestProcedures_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StandardTestProcedures_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StandardTestProcedures_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StandardTestProcedures_CreatedById",
                table: "StandardTestProcedures",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StandardTestProcedures_LastDeletedById",
                table: "StandardTestProcedures",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StandardTestProcedures_LastUpdatedById",
                table: "StandardTestProcedures",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StandardTestProcedures_MaterialId",
                table: "StandardTestProcedures",
                column: "MaterialId");
        }
    }
}
