using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class PendingCertificateChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateNotes",
                table: "Responses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoaReturnKind",
                table: "Responses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoaReturnReason",
                table: "Responses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoaVersion",
                table: "Responses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PendingCertificateChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedReturn = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreviousStateJson = table.Column<string>(type: "text", nullable: true),
                    NewStateJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingCertificateChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingCertificateChanges_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingCertificateChanges_ResponseId_Version",
                table: "PendingCertificateChanges",
                columns: new[] { "ResponseId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "PendingCertificateChanges") THEN
                        RAISE EXCEPTION 'Certificate revision history must be preserved before reverting this migration.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "PendingCertificateChanges");

            migrationBuilder.DropColumn(
                name: "CertificateNotes",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "CoaReturnKind",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "CoaReturnReason",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "CoaVersion",
                table: "Responses");
        }
    }
}
