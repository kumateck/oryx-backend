using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddWhitePlaceboPreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWhitePlacebo",
                table: "Materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WhitePlaceboPreparations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeRevision = table.Column<int>(type: "integer", nullable: false),
                    RecipeReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StorageConditions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HoldHours = table.Column<int>(type: "integer", nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OutputNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreparedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecordedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OutputBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhitePlaceboPreparations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparations_MaterialBatches_OutputBatchId",
                        column: x => x.OutputBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparations_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparations_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparations_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WhitePlaceboRecipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StorageConditions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    HoldHours = table.Column<int>(type: "integer", nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhitePlaceboRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipes_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipes_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WhitePlaceboPreparationInput",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WhitePlaceboPreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceShelfId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MaterialCode = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    UnitSymbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RecipeQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhitePlaceboPreparationInput", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparationInput_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparationInput_WhitePlaceboPreparations_White~",
                        column: x => x.WhitePlaceboPreparationId,
                        principalTable: "WhitePlaceboPreparations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparationInput_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparationInput_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WhitePlaceboPreparationInput_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WhitePlaceboRecipeIngredient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WhitePlaceboRecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhitePlaceboRecipeIngredient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipeIngredient_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipeIngredient_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipeIngredient_WhitePlaceboRecipes_WhitePlace~",
                        column: x => x.WhitePlaceboRecipeId,
                        principalTable: "WhitePlaceboRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipeIngredient_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipeIngredient_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WhitePlaceboRecipeIngredient_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparationInput_CreatedById",
                table: "WhitePlaceboPreparationInput",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparationInput_LastDeletedById",
                table: "WhitePlaceboPreparationInput",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparationInput_LastUpdatedById",
                table: "WhitePlaceboPreparationInput",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparationInput_MaterialBatchId",
                table: "WhitePlaceboPreparationInput",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparationInput_WhitePlaceboPreparationId",
                table: "WhitePlaceboPreparationInput",
                column: "WhitePlaceboPreparationId");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparations_CreatedById",
                table: "WhitePlaceboPreparations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparations_LastDeletedById",
                table: "WhitePlaceboPreparations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparations_LastUpdatedById",
                table: "WhitePlaceboPreparations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparations_MaterialId_BatchNumber",
                table: "WhitePlaceboPreparations",
                columns: new[] { "MaterialId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparations_OutputBatchId",
                table: "WhitePlaceboPreparations",
                column: "OutputBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparations_RequisitionId",
                table: "WhitePlaceboPreparations",
                column: "RequisitionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboPreparations_UoMId",
                table: "WhitePlaceboPreparations",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipeIngredient_CreatedById",
                table: "WhitePlaceboRecipeIngredient",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipeIngredient_LastDeletedById",
                table: "WhitePlaceboRecipeIngredient",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipeIngredient_LastUpdatedById",
                table: "WhitePlaceboRecipeIngredient",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipeIngredient_MaterialId",
                table: "WhitePlaceboRecipeIngredient",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipeIngredient_UoMId",
                table: "WhitePlaceboRecipeIngredient",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipeIngredient_WhitePlaceboRecipeId",
                table: "WhitePlaceboRecipeIngredient",
                column: "WhitePlaceboRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipes_CreatedById",
                table: "WhitePlaceboRecipes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipes_DepartmentId",
                table: "WhitePlaceboRecipes",
                column: "DepartmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipes_LastDeletedById",
                table: "WhitePlaceboRecipes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipes_LastUpdatedById",
                table: "WhitePlaceboRecipes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipes_MaterialId",
                table: "WhitePlaceboRecipes",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_WhitePlaceboRecipes_UoMId",
                table: "WhitePlaceboRecipes",
                column: "UoMId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhitePlaceboPreparationInput");

            migrationBuilder.DropTable(
                name: "WhitePlaceboRecipeIngredient");

            migrationBuilder.DropTable(
                name: "WhitePlaceboPreparations");

            migrationBuilder.DropTable(
                name: "WhitePlaceboRecipes");

            migrationBuilder.DropColumn(
                name: "IsWhitePlacebo",
                table: "Materials");
        }
    }
}
