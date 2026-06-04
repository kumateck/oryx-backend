using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShelfMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequestAssignee_AnalyticalTestRequests_Analyt~",
                table: "AnalyticalTestRequestAssignee");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequestAssignee_users_CreatedById",
                table: "AnalyticalTestRequestAssignee");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequestAssignee_users_LastDeletedById",
                table: "AnalyticalTestRequestAssignee");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequestAssignee_users_LastUpdatedById",
                table: "AnalyticalTestRequestAssignee");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequestAssignee_users_UserId",
                table: "AnalyticalTestRequestAssignee");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AnalyticalTestRequestAssignee",
                table: "AnalyticalTestRequestAssignee");

            migrationBuilder.RenameTable(
                name: "AnalyticalTestRequestAssignee",
                newName: "AnalyticalTestRequestAssignees");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequestAssignee_UserId",
                table: "AnalyticalTestRequestAssignees",
                newName: "IX_AnalyticalTestRequestAssignees_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequestAssignee_LastUpdatedById",
                table: "AnalyticalTestRequestAssignees",
                newName: "IX_AnalyticalTestRequestAssignees_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequestAssignee_LastDeletedById",
                table: "AnalyticalTestRequestAssignees",
                newName: "IX_AnalyticalTestRequestAssignees_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequestAssignee_CreatedById",
                table: "AnalyticalTestRequestAssignees",
                newName: "IX_AnalyticalTestRequestAssignees_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequestAssignee_AnalyticalTestRequestId",
                table: "AnalyticalTestRequestAssignees",
                newName: "IX_AnalyticalTestRequestAssignees_AnalyticalTestRequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AnalyticalTestRequestAssignees",
                table: "AnalyticalTestRequestAssignees",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequestAssignees_AnalyticalTestRequests_Analy~",
                table: "AnalyticalTestRequestAssignees",
                column: "AnalyticalTestRequestId",
                principalTable: "AnalyticalTestRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequestAssignees_users_CreatedById",
                table: "AnalyticalTestRequestAssignees",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequestAssignees_users_LastDeletedById",
                table: "AnalyticalTestRequestAssignees",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequestAssignees_users_LastUpdatedById",
                table: "AnalyticalTestRequestAssignees",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequestAssignees_users_UserId",
                table: "AnalyticalTestRequestAssignees",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequestAssignees_AnalyticalTestRequests_Analy~",
                table: "AnalyticalTestRequestAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequestAssignees_users_CreatedById",
                table: "AnalyticalTestRequestAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequestAssignees_users_LastDeletedById",
                table: "AnalyticalTestRequestAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequestAssignees_users_LastUpdatedById",
                table: "AnalyticalTestRequestAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticalTestRequestAssignees_users_UserId",
                table: "AnalyticalTestRequestAssignees");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AnalyticalTestRequestAssignees",
                table: "AnalyticalTestRequestAssignees");

            migrationBuilder.RenameTable(
                name: "AnalyticalTestRequestAssignees",
                newName: "AnalyticalTestRequestAssignee");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequestAssignees_UserId",
                table: "AnalyticalTestRequestAssignee",
                newName: "IX_AnalyticalTestRequestAssignee_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequestAssignees_LastUpdatedById",
                table: "AnalyticalTestRequestAssignee",
                newName: "IX_AnalyticalTestRequestAssignee_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequestAssignees_LastDeletedById",
                table: "AnalyticalTestRequestAssignee",
                newName: "IX_AnalyticalTestRequestAssignee_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequestAssignees_CreatedById",
                table: "AnalyticalTestRequestAssignee",
                newName: "IX_AnalyticalTestRequestAssignee_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalTestRequestAssignees_AnalyticalTestRequestId",
                table: "AnalyticalTestRequestAssignee",
                newName: "IX_AnalyticalTestRequestAssignee_AnalyticalTestRequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AnalyticalTestRequestAssignee",
                table: "AnalyticalTestRequestAssignee",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequestAssignee_AnalyticalTestRequests_Analyt~",
                table: "AnalyticalTestRequestAssignee",
                column: "AnalyticalTestRequestId",
                principalTable: "AnalyticalTestRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequestAssignee_users_CreatedById",
                table: "AnalyticalTestRequestAssignee",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequestAssignee_users_LastDeletedById",
                table: "AnalyticalTestRequestAssignee",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequestAssignee_users_LastUpdatedById",
                table: "AnalyticalTestRequestAssignee",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticalTestRequestAssignee_users_UserId",
                table: "AnalyticalTestRequestAssignee",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
