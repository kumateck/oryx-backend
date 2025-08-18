using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class CollapseClaimToLabelClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Claim",
                table: "ProductSpecifications");

            migrationBuilder.RenameColumn(
                name: "Label",
                table: "ProductSpecifications",
                newName: "LabelClaim");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LabelClaim",
                table: "ProductSpecifications",
                newName: "Label");

            migrationBuilder.AddColumn<string>(
                name: "Claim",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);
        }
    }
}
