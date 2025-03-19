using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Added_ContainerPackageStyle_BillingSheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingSheets_Suppliers_SupplierId",
                table: "BillingSheets");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "BillingSheets",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ContainerPackageStyleId",
                table: "BillingSheets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheets_ContainerPackageStyleId",
                table: "BillingSheets",
                column: "ContainerPackageStyleId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingSheets_PackageStyles_ContainerPackageStyleId",
                table: "BillingSheets",
                column: "ContainerPackageStyleId",
                principalTable: "PackageStyles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingSheets_Suppliers_SupplierId",
                table: "BillingSheets",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingSheets_PackageStyles_ContainerPackageStyleId",
                table: "BillingSheets");

            migrationBuilder.DropForeignKey(
                name: "FK_BillingSheets_Suppliers_SupplierId",
                table: "BillingSheets");

            migrationBuilder.DropIndex(
                name: "IX_BillingSheets_ContainerPackageStyleId",
                table: "BillingSheets");

            migrationBuilder.DropColumn(
                name: "ContainerPackageStyleId",
                table: "BillingSheets");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "BillingSheets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingSheets_Suppliers_SupplierId",
                table: "BillingSheets",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
