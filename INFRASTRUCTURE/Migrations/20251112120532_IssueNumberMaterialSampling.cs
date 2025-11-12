using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class IssueNumberMaterialSampling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ArNumber",
                table: "MaterialSamplings",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueNumber",
                table: "MaterialSamplings",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "MaterialSamplings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IssuedById",
                table: "MaterialSamplings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSamplings_IssuedById",
                table: "MaterialSamplings",
                column: "IssuedById");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSamplings_users_IssuedById",
                table: "MaterialSamplings",
                column: "IssuedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSamplings_users_IssuedById",
                table: "MaterialSamplings");

            migrationBuilder.DropIndex(
                name: "IX_MaterialSamplings_IssuedById",
                table: "MaterialSamplings");

            migrationBuilder.DropColumn(
                name: "IssueNumber",
                table: "MaterialSamplings");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "MaterialSamplings");

            migrationBuilder.DropColumn(
                name: "IssuedById",
                table: "MaterialSamplings");

            migrationBuilder.AlterColumn<string>(
                name: "ArNumber",
                table: "MaterialSamplings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000000)",
                oldMaxLength: 1000000,
                oldNullable: true);
        }
    }
}
