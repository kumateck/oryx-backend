using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentAndUpdateFormSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InstrumentId",
                table: "FormSections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "FormSections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Instruments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Instruments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Instruments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_InstrumentId",
                table: "FormSections",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_CreatedById",
                table: "Instruments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_LastDeletedById",
                table: "Instruments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_LastUpdatedById",
                table: "Instruments",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_FormSections_Instruments_InstrumentId",
                table: "FormSections",
                column: "InstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormSections_Instruments_InstrumentId",
                table: "FormSections");

            migrationBuilder.DropTable(
                name: "Instruments");

            migrationBuilder.DropIndex(
                name: "IX_FormSections_InstrumentId",
                table: "FormSections");

            migrationBuilder.DropColumn(
                name: "InstrumentId",
                table: "FormSections");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "FormSections");
        }
    }
}
