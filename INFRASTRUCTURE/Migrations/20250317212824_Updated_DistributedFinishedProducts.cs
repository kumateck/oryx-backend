using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_DistributedFinishedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TransferNoteId",
                table: "DistributedFinishedProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistributedFinishedProducts_TransferNoteId",
                table: "DistributedFinishedProducts",
                column: "TransferNoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedFinishedProducts_FinishedGoodsTransferNotes_Tran~",
                table: "DistributedFinishedProducts",
                column: "TransferNoteId",
                principalTable: "FinishedGoodsTransferNotes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedFinishedProducts_FinishedGoodsTransferNotes_Tran~",
                table: "DistributedFinishedProducts");

            migrationBuilder.DropIndex(
                name: "IX_DistributedFinishedProducts_TransferNoteId",
                table: "DistributedFinishedProducts");

            migrationBuilder.DropColumn(
                name: "TransferNoteId",
                table: "DistributedFinishedProducts");
        }
    }
}
