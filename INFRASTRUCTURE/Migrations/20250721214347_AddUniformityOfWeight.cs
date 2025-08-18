using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddUniformityOfWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UniformityOfWeightId",
                table: "MaterialAnalyticalRawData",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UniformityOfWeights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BalanceNumber = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NumberOfItems = table.Column<int>(type: "integer", nullable: false),
                    NominalWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DisintegrationTest = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DisintegrationInstrumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisintegrationMean = table.Column<decimal>(type: "numeric", nullable: false),
                    HardnessTest = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    HardnessInstrumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    HardnessMean = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniformityOfWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UniformityOfWeights_Instruments_DisintegrationInstrumentId",
                        column: x => x.DisintegrationInstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UniformityOfWeights_Instruments_HardnessInstrumentId",
                        column: x => x.HardnessInstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UniformityOfWeights_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UniformityOfWeights_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UniformityOfWeights_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UniformityOfWeightResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UniformityOfWeightId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Weights = table.Column<List<decimal>>(type: "numeric[]", nullable: true),
                    Mean = table.Column<decimal>(type: "numeric", nullable: false),
                    StandardDeviation = table.Column<decimal>(type: "numeric", nullable: false),
                    MinimumStandardDeviation = table.Column<decimal>(type: "numeric", nullable: false),
                    MaximumStandardDeviation = table.Column<decimal>(type: "numeric", nullable: false),
                    MaximumWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    MinimumWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniformityOfWeightResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UniformityOfWeightResponses_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UniformityOfWeightResponses_UniformityOfWeights_UniformityO~",
                        column: x => x.UniformityOfWeightId,
                        principalTable: "UniformityOfWeights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UniformityOfWeightResponses_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UniformityOfWeightResponses_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UniformityOfWeightResponses_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialAnalyticalRawData_UniformityOfWeightId",
                table: "MaterialAnalyticalRawData",
                column: "UniformityOfWeightId");

            migrationBuilder.CreateIndex(
                name: "IX_UniformityOfWeightResponses_CreatedById",
                table: "UniformityOfWeightResponses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_UniformityOfWeightResponses_LastDeletedById",
                table: "UniformityOfWeightResponses",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_UniformityOfWeightResponses_LastUpdatedById",
                table: "UniformityOfWeightResponses",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_UniformityOfWeightResponses_MaterialBatchId",
                table: "UniformityOfWeightResponses",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_UniformityOfWeightResponses_UniformityOfWeightId",
                table: "UniformityOfWeightResponses",
                column: "UniformityOfWeightId");

            migrationBuilder.CreateIndex(
                name: "IX_UniformityOfWeights_CreatedById",
                table: "UniformityOfWeights",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_UniformityOfWeights_DisintegrationInstrumentId",
                table: "UniformityOfWeights",
                column: "DisintegrationInstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UniformityOfWeights_HardnessInstrumentId",
                table: "UniformityOfWeights",
                column: "HardnessInstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UniformityOfWeights_LastDeletedById",
                table: "UniformityOfWeights",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_UniformityOfWeights_LastUpdatedById",
                table: "UniformityOfWeights",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialAnalyticalRawData_UniformityOfWeights_UniformityOfW~",
                table: "MaterialAnalyticalRawData",
                column: "UniformityOfWeightId",
                principalTable: "UniformityOfWeights",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialAnalyticalRawData_UniformityOfWeights_UniformityOfW~",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropTable(
                name: "UniformityOfWeightResponses");

            migrationBuilder.DropTable(
                name: "UniformityOfWeights");

            migrationBuilder.DropIndex(
                name: "IX_MaterialAnalyticalRawData_UniformityOfWeightId",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "UniformityOfWeightId",
                table: "MaterialAnalyticalRawData");
        }
    }
}
