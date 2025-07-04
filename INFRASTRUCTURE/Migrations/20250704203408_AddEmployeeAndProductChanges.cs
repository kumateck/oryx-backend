using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeAndProductChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPackages_UnitOfMeasures_BaseUoMId",
                table: "ProductPackages");

            migrationBuilder.DropIndex(
                name: "IX_ProductPackages_BaseUoMId",
                table: "ProductPackages");

            migrationBuilder.DropColumn(
                name: "BaseUoMId",
                table: "ProductPackages");

            migrationBuilder.AddColumn<int>(
                name: "Division",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PackPerShipper",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ActiveStatus",
                table: "Employees",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InactiveStatus",
                table: "Employees",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensionEndDate",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensionStartDate",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Division",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PackPerShipper",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ActiveStatus",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "InactiveStatus",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SuspensionEndDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SuspensionStartDate",
                table: "Employees");

            migrationBuilder.AddColumn<Guid>(
                name: "BaseUoMId",
                table: "ProductPackages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_BaseUoMId",
                table: "ProductPackages",
                column: "BaseUoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPackages_UnitOfMeasures_BaseUoMId",
                table: "ProductPackages",
                column: "BaseUoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }
    }
}
