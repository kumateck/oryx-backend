using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class LeaveTypeDesignationRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesignationList",
                table: "LeaveTypes");

            migrationBuilder.CreateTable(
                name: "DesignationLeaveType",
                columns: table => new
                {
                    DesignationsId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveTypesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignationLeaveType", x => new { x.DesignationsId, x.LeaveTypesId });
                    table.ForeignKey(
                        name: "FK_DesignationLeaveType_Designations_DesignationListId",
                        column: x => x.DesignationsId,
                        principalTable: "Designations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DesignationLeaveType_LeaveTypes_LeaveTypesId",
                        column: x => x.LeaveTypesId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DesignationLeaveType_LeaveTypesId",
                table: "DesignationLeaveType",
                column: "LeaveTypesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DesignationLeaveType");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "DesignationList",
                table: "LeaveTypes",
                type: "uuid[]",
                nullable: true);
        }
    }
}
