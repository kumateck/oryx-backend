using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionScheduleItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItem_Materials_MaterialId",
                table: "ProductionScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItem_ProductionSchedules_ProductionSchedu~",
                table: "ProductionScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItem_UnitOfMeasures_UomId",
                table: "ProductionScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItem_users_CreatedById",
                table: "ProductionScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItem_users_LastDeletedById",
                table: "ProductionScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItem_users_LastUpdatedById",
                table: "ProductionScheduleItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductionScheduleItem",
                table: "ProductionScheduleItem");

            migrationBuilder.RenameTable(
                name: "ProductionScheduleItem",
                newName: "ProductionScheduleItems");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItem_UomId",
                table: "ProductionScheduleItems",
                newName: "IX_ProductionScheduleItems_UomId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItem_ProductionScheduleId",
                table: "ProductionScheduleItems",
                newName: "IX_ProductionScheduleItems_ProductionScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItem_MaterialId",
                table: "ProductionScheduleItems",
                newName: "IX_ProductionScheduleItems_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItem_LastUpdatedById",
                table: "ProductionScheduleItems",
                newName: "IX_ProductionScheduleItems_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItem_LastDeletedById",
                table: "ProductionScheduleItems",
                newName: "IX_ProductionScheduleItems_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItem_CreatedById",
                table: "ProductionScheduleItems",
                newName: "IX_ProductionScheduleItems_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductionScheduleItems",
                table: "ProductionScheduleItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItems_Materials_MaterialId",
                table: "ProductionScheduleItems",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItems_ProductionSchedules_ProductionSched~",
                table: "ProductionScheduleItems",
                column: "ProductionScheduleId",
                principalTable: "ProductionSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItems_UnitOfMeasures_UomId",
                table: "ProductionScheduleItems",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItems_users_CreatedById",
                table: "ProductionScheduleItems",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItems_users_LastDeletedById",
                table: "ProductionScheduleItems",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItems_users_LastUpdatedById",
                table: "ProductionScheduleItems",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItems_Materials_MaterialId",
                table: "ProductionScheduleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItems_ProductionSchedules_ProductionSched~",
                table: "ProductionScheduleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItems_UnitOfMeasures_UomId",
                table: "ProductionScheduleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItems_users_CreatedById",
                table: "ProductionScheduleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItems_users_LastDeletedById",
                table: "ProductionScheduleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionScheduleItems_users_LastUpdatedById",
                table: "ProductionScheduleItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductionScheduleItems",
                table: "ProductionScheduleItems");

            migrationBuilder.RenameTable(
                name: "ProductionScheduleItems",
                newName: "ProductionScheduleItem");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItems_UomId",
                table: "ProductionScheduleItem",
                newName: "IX_ProductionScheduleItem_UomId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItems_ProductionScheduleId",
                table: "ProductionScheduleItem",
                newName: "IX_ProductionScheduleItem_ProductionScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItems_MaterialId",
                table: "ProductionScheduleItem",
                newName: "IX_ProductionScheduleItem_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItems_LastUpdatedById",
                table: "ProductionScheduleItem",
                newName: "IX_ProductionScheduleItem_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItems_LastDeletedById",
                table: "ProductionScheduleItem",
                newName: "IX_ProductionScheduleItem_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_ProductionScheduleItems_CreatedById",
                table: "ProductionScheduleItem",
                newName: "IX_ProductionScheduleItem_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductionScheduleItem",
                table: "ProductionScheduleItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItem_Materials_MaterialId",
                table: "ProductionScheduleItem",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItem_ProductionSchedules_ProductionSchedu~",
                table: "ProductionScheduleItem",
                column: "ProductionScheduleId",
                principalTable: "ProductionSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItem_UnitOfMeasures_UomId",
                table: "ProductionScheduleItem",
                column: "UomId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItem_users_CreatedById",
                table: "ProductionScheduleItem",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItem_users_LastDeletedById",
                table: "ProductionScheduleItem",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionScheduleItem_users_LastUpdatedById",
                table: "ProductionScheduleItem",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
