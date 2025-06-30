using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCreateRouteRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteOperationAction");

            migrationBuilder.AddColumn<int>(
                name: "Action",
                table: "RouteResponsibleUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductAnalyticalRawDataId",
                table: "RouteResponsibleUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Action",
                table: "RouteResponsibleRoles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductAnalyticalRawDataId",
                table: "RouteResponsibleRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Action",
                table: "ProductionActivityStepUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductAnalyticalRawDataId",
                table: "ProductionActivityStepUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_ProductAnalyticalRawDataId",
                table: "RouteResponsibleUsers",
                column: "ProductAnalyticalRawDataId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_ProductAnalyticalRawDataId",
                table: "RouteResponsibleRoles",
                column: "ProductAnalyticalRawDataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_ProductAnalyticalRawDataId",
                table: "ProductionActivityStepUsers",
                column: "ProductAnalyticalRawDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionActivityStepUsers_ProductAnalyticalRawData_Produc~",
                table: "ProductionActivityStepUsers",
                column: "ProductAnalyticalRawDataId",
                principalTable: "ProductAnalyticalRawData",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteResponsibleRoles_ProductAnalyticalRawData_ProductAnaly~",
                table: "RouteResponsibleRoles",
                column: "ProductAnalyticalRawDataId",
                principalTable: "ProductAnalyticalRawData",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteResponsibleUsers_ProductAnalyticalRawData_ProductAnaly~",
                table: "RouteResponsibleUsers",
                column: "ProductAnalyticalRawDataId",
                principalTable: "ProductAnalyticalRawData",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionActivityStepUsers_ProductAnalyticalRawData_Produc~",
                table: "ProductionActivityStepUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteResponsibleRoles_ProductAnalyticalRawData_ProductAnaly~",
                table: "RouteResponsibleRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteResponsibleUsers_ProductAnalyticalRawData_ProductAnaly~",
                table: "RouteResponsibleUsers");

            migrationBuilder.DropIndex(
                name: "IX_RouteResponsibleUsers_ProductAnalyticalRawDataId",
                table: "RouteResponsibleUsers");

            migrationBuilder.DropIndex(
                name: "IX_RouteResponsibleRoles_ProductAnalyticalRawDataId",
                table: "RouteResponsibleRoles");

            migrationBuilder.DropIndex(
                name: "IX_ProductionActivityStepUsers_ProductAnalyticalRawDataId",
                table: "ProductionActivityStepUsers");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "RouteResponsibleUsers");

            migrationBuilder.DropColumn(
                name: "ProductAnalyticalRawDataId",
                table: "RouteResponsibleUsers");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "RouteResponsibleRoles");

            migrationBuilder.DropColumn(
                name: "ProductAnalyticalRawDataId",
                table: "RouteResponsibleRoles");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "ProductionActivityStepUsers");

            migrationBuilder.DropColumn(
                name: "ProductAnalyticalRawDataId",
                table: "ProductionActivityStepUsers");

            migrationBuilder.CreateTable(
                name: "RouteOperationAction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductAnalyticalRawDataId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ProductionActivityStepUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RouteResponsibleRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RouteResponsibleUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteOperationAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteOperationAction_ProductAnalyticalRawData_ProductAnalyt~",
                        column: x => x.ProductAnalyticalRawDataId,
                        principalTable: "ProductAnalyticalRawData",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteOperationAction_ProductionActivityStepUsers_Production~",
                        column: x => x.ProductionActivityStepUserId,
                        principalTable: "ProductionActivityStepUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteOperationAction_RouteResponsibleRoles_RouteResponsible~",
                        column: x => x.RouteResponsibleRoleId,
                        principalTable: "RouteResponsibleRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteOperationAction_RouteResponsibleUsers_RouteResponsible~",
                        column: x => x.RouteResponsibleUserId,
                        principalTable: "RouteResponsibleUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteOperationAction_ProductAnalyticalRawDataId",
                table: "RouteOperationAction",
                column: "ProductAnalyticalRawDataId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteOperationAction_ProductionActivityStepUserId",
                table: "RouteOperationAction",
                column: "ProductionActivityStepUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteOperationAction_RouteResponsibleRoleId",
                table: "RouteOperationAction",
                column: "RouteResponsibleRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteOperationAction_RouteResponsibleUserId",
                table: "RouteOperationAction",
                column: "RouteResponsibleUserId");
        }
    }
}
