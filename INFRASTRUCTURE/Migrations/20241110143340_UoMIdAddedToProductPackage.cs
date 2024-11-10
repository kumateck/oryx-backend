using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UoMIdAddedToProductPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UoMId",
                table: "ProductPackages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_UoMId",
                table: "ProductPackages",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_UnitOfMeasures_UoMId",
                table: "ProductPackages",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_UnitOfMeasures_UoMId",
                table: "ProductPackages");

            migrationBuilder.DropIndex(
                name: "IX_ProductPackages_UoMId",
                table: "ProductPackages");

            migrationBuilder.DropColumn(
                name: "UoMId",
                table: "ProductPackages");
        }
    }
}
