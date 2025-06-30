using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeEmployeeIdToEmployeeOvertime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeIds",
                table: "OvertimeRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "OvertimeRequestId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<string>(type: "text", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WorkState = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OvertimeRequestId",
                table: "Employees",
                column: "OvertimeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_CreatedById",
                table: "AttendanceRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_LastDeletedById",
                table: "AttendanceRecords",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_LastUpdatedById",
                table: "AttendanceRecords",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_OvertimeRequests_OvertimeRequestId",
                table: "Employees",
                column: "OvertimeRequestId",
                principalTable: "OvertimeRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_OvertimeRequests_OvertimeRequestId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_Employees_OvertimeRequestId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "OvertimeRequestId",
                table: "Employees");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "EmployeeIds",
                table: "OvertimeRequests",
                type: "uuid[]",
                nullable: true);
        }
    }
}
