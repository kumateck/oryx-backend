using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationFlagToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "ProductSpecifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "ProductSpecifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedById",
                table: "ProductSpecifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedById",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "ProductAnalyticalRawData",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "ProductAnalyticalRawData",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedById",
                table: "ProductAnalyticalRawData",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "MaterialSpecifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "MaterialSpecifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedById",
                table: "MaterialSpecifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "MaterialAnalyticalRawData",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "MaterialAnalyticalRawData",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedById",
                table: "MaterialAnalyticalRawData",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "VerifiedById",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VerifiedById",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "ProductAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "ProductAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "VerifiedById",
                table: "ProductAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "MaterialSpecifications");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "MaterialSpecifications");

            migrationBuilder.DropColumn(
                name: "VerifiedById",
                table: "MaterialSpecifications");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "MaterialAnalyticalRawData");

            migrationBuilder.DropColumn(
                name: "VerifiedById",
                table: "MaterialAnalyticalRawData");
        }
    }
}
