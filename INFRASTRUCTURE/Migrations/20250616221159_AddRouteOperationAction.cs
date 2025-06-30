using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteOperationAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RouteOperationAction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ProductionActivityStepUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RouteResponsibleRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RouteResponsibleUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteOperationAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteOperationAction_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
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
                name: "IX_RouteOperationAction_FormId",
                table: "RouteOperationAction",
                column: "FormId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteOperationAction");
        }
    }
}
