using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantitiesToFGTN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinishedGoodsTransferNoteQuantity",
                columns: table => new
                {
                    FinishedGoodsTransferNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    MovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MovedById = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodsTransferNoteQuantity", x => new { x.FinishedGoodsTransferNoteId, x.Id });
                    table.ForeignKey(
                        name: "FK_FinishedGoodsTransferNoteQuantity_FinishedGoodsTransferNote~",
                        column: x => x.FinishedGoodsTransferNoteId,
                        principalTable: "FinishedGoodsTransferNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinishedGoodsTransferNoteQuantity");
        }
    }
}
