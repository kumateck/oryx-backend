using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_DistributedMaterial_Status : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmArrival",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "GrnGenerated",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "IsChecked",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "DistributedRequisitionMaterials",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmArrival",
                table: "DistributedRequisitionMaterials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GrnGenerated",
                table: "DistributedRequisitionMaterials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsChecked",
                table: "DistributedRequisitionMaterials",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
