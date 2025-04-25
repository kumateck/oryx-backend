using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignationLeaveType_Designations_DesignationListId",
                table: "DesignationLeaveType");

            migrationBuilder.RenameColumn(
                name: "DesignationListId",
                table: "DesignationLeaveType",
                newName: "DesignationsId");

            migrationBuilder.CreateTable(
                name: "ShiftTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftName = table.Column<string>(type: "text", nullable: true),
                    RotationType = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApplicableDays = table.Column<int[]>(type: "integer[]", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftTypes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShiftTypes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShiftTypes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftTypes_CreatedById",
                table: "ShiftTypes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftTypes_LastDeletedById",
                table: "ShiftTypes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftTypes_LastUpdatedById",
                table: "ShiftTypes",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignationLeaveType_Designations_DesignationsId",
                table: "DesignationLeaveType",
                column: "DesignationsId",
                principalTable: "Designations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignationLeaveType_Designations_DesignationsId",
                table: "DesignationLeaveType");

            migrationBuilder.DropTable(
                name: "ShiftTypes");

            migrationBuilder.RenameColumn(
                name: "DesignationsId",
                table: "DesignationLeaveType",
                newName: "DesignationListId");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignationLeaveType_Designations_DesignationListId",
                table: "DesignationLeaveType",
                column: "DesignationListId",
                principalTable: "Designations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
