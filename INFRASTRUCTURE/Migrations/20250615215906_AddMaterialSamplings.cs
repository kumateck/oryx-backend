using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialSamplings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArNumber",
                table: "ProductSamplings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SampleDate",
                table: "ProductSamplings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "MaterialSamplings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArNumber = table.Column<string>(type: "text", nullable: true),
                    GrnId = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleQuantity = table.Column<string>(type: "text", nullable: true),
                    SampleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialSamplings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialSamplings_Grns_GrnId",
                        column: x => x.GrnId,
                        principalTable: "Grns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialSamplings_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialSamplings_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialSamplings_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSamplings_AnalyticalTestRequestId",
                table: "ProductSamplings",
                column: "AnalyticalTestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSamplings_CreatedById",
                table: "MaterialSamplings",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSamplings_GrnId",
                table: "MaterialSamplings",
                column: "GrnId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSamplings_LastDeletedById",
                table: "MaterialSamplings",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSamplings_LastUpdatedById",
                table: "MaterialSamplings",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSamplings_AnalyticalTestRequests_AnalyticalTestReque~",
                table: "ProductSamplings",
                column: "AnalyticalTestRequestId",
                principalTable: "AnalyticalTestRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSamplings_AnalyticalTestRequests_AnalyticalTestReque~",
                table: "ProductSamplings");

            migrationBuilder.DropTable(
                name: "MaterialSamplings");

            migrationBuilder.DropIndex(
                name: "IX_ProductSamplings_AnalyticalTestRequestId",
                table: "ProductSamplings");

            migrationBuilder.DropColumn(
                name: "ArNumber",
                table: "ProductSamplings");

            migrationBuilder.DropColumn(
                name: "SampleDate",
                table: "ProductSamplings");
        }
    }
}
