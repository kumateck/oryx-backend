using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributedRequisitionItemToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_RequisitionItems_Requisitio~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributeMaterials_DistributedRequisitionMaterials_Distrib~",
                table: "DistributeMaterials");

            migrationBuilder.DropIndex(
                name: "IX_DistributedRequisitionMaterials_RequisitionItemId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "RequisitionItemId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.RenameColumn(
                name: "DistributedRequisitionMaterialId",
                table: "DistributeMaterials",
                newName: "DistributedRequisitionItemId");

            migrationBuilder.RenameIndex(
                name: "IX_DistributeMaterials_DistributedRequisitionMaterialId",
                table: "DistributeMaterials",
                newName: "IX_DistributeMaterials_DistributedRequisitionItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributeMaterials_DistributedRequisitionItem_DistributedR~",
                table: "DistributeMaterials",
                column: "DistributedRequisitionItemId",
                principalTable: "DistributedRequisitionItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributeMaterials_DistributedRequisitionItem_DistributedR~",
                table: "DistributeMaterials");

            migrationBuilder.RenameColumn(
                name: "DistributedRequisitionItemId",
                table: "DistributeMaterials",
                newName: "DistributedRequisitionMaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_DistributeMaterials_DistributedRequisitionItemId",
                table: "DistributeMaterials",
                newName: "IX_DistributeMaterials_DistributedRequisitionMaterialId");

            migrationBuilder.AddColumn<Guid>(
                name: "RequisitionItemId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_RequisitionItemId",
                table: "DistributedRequisitionMaterials",
                column: "RequisitionItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_RequisitionItems_Requisitio~",
                table: "DistributedRequisitionMaterials",
                column: "RequisitionItemId",
                principalTable: "RequisitionItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributeMaterials_DistributedRequisitionMaterials_Distrib~",
                table: "DistributeMaterials",
                column: "DistributedRequisitionMaterialId",
                principalTable: "DistributedRequisitionMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
