using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueNumberToProductSampling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ArNumber",
                table: "ProductSamplings",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueNumber",
                table: "ProductSamplings",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "ProductSamplings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IssuedById",
                table: "ProductSamplings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSamplings_IssuedById",
                table: "ProductSamplings",
                column: "IssuedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSamplings_users_IssuedById",
                table: "ProductSamplings",
                column: "IssuedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSamplings_users_IssuedById",
                table: "ProductSamplings");

            migrationBuilder.DropIndex(
                name: "IX_ProductSamplings_IssuedById",
                table: "ProductSamplings");

            migrationBuilder.DropColumn(
                name: "IssueNumber",
                table: "ProductSamplings");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "ProductSamplings");

            migrationBuilder.DropColumn(
                name: "IssuedById",
                table: "ProductSamplings");

            migrationBuilder.AlterColumn<string>(
                name: "ArNumber",
                table: "ProductSamplings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000000)",
                oldMaxLength: 1000000,
                oldNullable: true);
        }
    }
}
