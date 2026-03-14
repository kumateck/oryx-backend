using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeInstriumentToQcEquipmentinFormSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormSections_Instruments_InstrumentId",
                table: "FormSections");

            migrationBuilder.AddForeignKey(
                name: "FK_FormSections_QcEquipments_InstrumentId",
                table: "FormSections",
                column: "InstrumentId",
                principalTable: "QcEquipments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormSections_QcEquipments_InstrumentId",
                table: "FormSections");

            migrationBuilder.AddForeignKey(
                name: "FK_FormSections_Instruments_InstrumentId",
                table: "FormSections",
                column: "InstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id");
        }
    }
}
