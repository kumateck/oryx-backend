using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBatchManufacturingRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ManufacturingDate",
                table: "BatchPackagingRecords",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiryDate",
                table: "BatchPackagingRecords",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "BatchNumber",
                table: "BatchPackagingRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionActivityStepId",
                table: "BatchPackagingRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionScheduleId",
                table: "BatchPackagingRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<DateTime>(
                name: "ManufacturingDate",
                table: "BatchManufacturingRecords",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiryDate",
                table: "BatchManufacturingRecords",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionActivityStepId",
                table: "BatchManufacturingRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionScheduleId",
                table: "BatchManufacturingRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_ProductionActivityStepId",
                table: "BatchPackagingRecords",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_ProductionScheduleId",
                table: "BatchPackagingRecords",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_ProductionActivityStepId",
                table: "BatchManufacturingRecords",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_ProductionScheduleId",
                table: "BatchManufacturingRecords",
                column: "ProductionScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_BatchManufacturingRecords_ProductionActivitySteps_Productio~",
                table: "BatchManufacturingRecords",
                column: "ProductionActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchManufacturingRecords_ProductionSchedules_ProductionSch~",
                table: "BatchManufacturingRecords",
                column: "ProductionScheduleId",
                principalTable: "ProductionSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchPackagingRecords_ProductionActivitySteps_ProductionAct~",
                table: "BatchPackagingRecords",
                column: "ProductionActivityStepId",
                principalTable: "ProductionActivitySteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchPackagingRecords_ProductionSchedules_ProductionSchedul~",
                table: "BatchPackagingRecords",
                column: "ProductionScheduleId",
                principalTable: "ProductionSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatchManufacturingRecords_ProductionActivitySteps_Productio~",
                table: "BatchManufacturingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchManufacturingRecords_ProductionSchedules_ProductionSch~",
                table: "BatchManufacturingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchPackagingRecords_ProductionActivitySteps_ProductionAct~",
                table: "BatchPackagingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchPackagingRecords_ProductionSchedules_ProductionSchedul~",
                table: "BatchPackagingRecords");

            migrationBuilder.DropIndex(
                name: "IX_BatchPackagingRecords_ProductionActivityStepId",
                table: "BatchPackagingRecords");

            migrationBuilder.DropIndex(
                name: "IX_BatchPackagingRecords_ProductionScheduleId",
                table: "BatchPackagingRecords");

            migrationBuilder.DropIndex(
                name: "IX_BatchManufacturingRecords_ProductionActivityStepId",
                table: "BatchManufacturingRecords");

            migrationBuilder.DropIndex(
                name: "IX_BatchManufacturingRecords_ProductionScheduleId",
                table: "BatchManufacturingRecords");

            migrationBuilder.DropColumn(
                name: "ProductionActivityStepId",
                table: "BatchPackagingRecords");

            migrationBuilder.DropColumn(
                name: "ProductionScheduleId",
                table: "BatchPackagingRecords");

            migrationBuilder.DropColumn(
                name: "ProductionActivityStepId",
                table: "BatchManufacturingRecords");

            migrationBuilder.DropColumn(
                name: "ProductionScheduleId",
                table: "BatchManufacturingRecords");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ManufacturingDate",
                table: "BatchPackagingRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiryDate",
                table: "BatchPackagingRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BatchNumber",
                table: "BatchPackagingRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ManufacturingDate",
                table: "BatchManufacturingRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiryDate",
                table: "BatchManufacturingRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
