using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialRetestSampling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSamplings_Grns_GrnId",
                table: "MaterialSamplings");

            migrationBuilder.AlterColumn<Guid>(
                name: "GrnId",
                table: "MaterialSamplings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialRetestRequestId",
                table: "MaterialSamplings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArNumber",
                table: "MaterialRetestRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSamplings_MaterialRetestRequestId",
                table: "MaterialSamplings",
                column: "MaterialRetestRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSamplings_Grns_GrnId",
                table: "MaterialSamplings",
                column: "GrnId",
                principalTable: "Grns",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSamplings_MaterialRetestRequests_MaterialRetestRequ~",
                table: "MaterialSamplings",
                column: "MaterialRetestRequestId",
                principalTable: "MaterialRetestRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSamplings_Grns_GrnId",
                table: "MaterialSamplings");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSamplings_MaterialRetestRequests_MaterialRetestRequ~",
                table: "MaterialSamplings");

            migrationBuilder.DropIndex(
                name: "IX_MaterialSamplings_MaterialRetestRequestId",
                table: "MaterialSamplings");

            migrationBuilder.DropColumn(
                name: "MaterialRetestRequestId",
                table: "MaterialSamplings");

            migrationBuilder.DropColumn(
                name: "ArNumber",
                table: "MaterialRetestRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "GrnId",
                table: "MaterialSamplings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSamplings_Grns_GrnId",
                table: "MaterialSamplings",
                column: "GrnId",
                principalTable: "Grns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
