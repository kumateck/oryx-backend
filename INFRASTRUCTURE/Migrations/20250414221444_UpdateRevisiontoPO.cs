using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRevisiontoPO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Departments_DepartmentId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Designations_DesignationId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_users_ReportingManagerId",
                table: "Employees");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyBeforeId",
                table: "RevisedPurchaseOrder",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialBeforeId",
                table: "RevisedPurchaseOrder",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceBefore",
                table: "RevisedPurchaseOrder",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityBefore",
                table: "RevisedPurchaseOrder",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UoMBeforeId",
                table: "RevisedPurchaseOrder",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ReportingManagerId",
                table: "Employees",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "DesignationId",
                table: "Employees",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "Employees",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrder_CurrencyBeforeId",
                table: "RevisedPurchaseOrder",
                column: "CurrencyBeforeId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrder_MaterialBeforeId",
                table: "RevisedPurchaseOrder",
                column: "MaterialBeforeId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrder_UoMBeforeId",
                table: "RevisedPurchaseOrder",
                column: "UoMBeforeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Departments_DepartmentId",
                table: "Employees",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Designations_DesignationId",
                table: "Employees",
                column: "DesignationId",
                principalTable: "Designations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_users_ReportingManagerId",
                table: "Employees",
                column: "ReportingManagerId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrder_Currencies_CurrencyBeforeId",
                table: "RevisedPurchaseOrder",
                column: "CurrencyBeforeId",
                principalTable: "Currencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrder_Materials_MaterialBeforeId",
                table: "RevisedPurchaseOrder",
                column: "MaterialBeforeId",
                principalTable: "Materials",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrder_UnitOfMeasures_UoMBeforeId",
                table: "RevisedPurchaseOrder",
                column: "UoMBeforeId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Departments_DepartmentId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Designations_DesignationId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_users_ReportingManagerId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrder_Currencies_CurrencyBeforeId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrder_Materials_MaterialBeforeId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrder_UnitOfMeasures_UoMBeforeId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropIndex(
                name: "IX_RevisedPurchaseOrder_CurrencyBeforeId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropIndex(
                name: "IX_RevisedPurchaseOrder_MaterialBeforeId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropIndex(
                name: "IX_RevisedPurchaseOrder_UoMBeforeId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "CurrencyBeforeId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "MaterialBeforeId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "PriceBefore",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "QuantityBefore",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "UoMBeforeId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReportingManagerId",
                table: "Employees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DesignationId",
                table: "Employees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "Employees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Departments_DepartmentId",
                table: "Employees",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Designations_DesignationId",
                table: "Employees",
                column: "DesignationId",
                principalTable: "Designations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_users_ReportingManagerId",
                table: "Employees",
                column: "ReportingManagerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
