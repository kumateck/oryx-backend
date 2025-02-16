using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Added_Grn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GrnId",
                table: "MaterialBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Grns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierName = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    VehicleNumber = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grns_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Grns_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Grns_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_GrnId",
                table: "MaterialBatches",
                column: "GrnId");

            migrationBuilder.CreateIndex(
                name: "IX_Grns_CreatedById",
                table: "Grns",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Grns_LastDeletedById",
                table: "Grns",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Grns_LastUpdatedById",
                table: "Grns",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_Grns_GrnId",
                table: "MaterialBatches",
                column: "GrnId",
                principalTable: "Grns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_Grns_GrnId",
                table: "MaterialBatches");

            migrationBuilder.DropTable(
                name: "Grns");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatches_GrnId",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "GrnId",
                table: "MaterialBatches");
        }
    }
}
