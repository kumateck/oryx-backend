using System;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261025110000_AddRelatedEntityContextToNotifications")]
public partial class AddRelatedEntityContextToNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddContextColumns(migrationBuilder, "AlertDispatchRequests");
        AddContextColumns(migrationBuilder, "Notifications");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropContextColumns(migrationBuilder, "Notifications");
        DropContextColumns(migrationBuilder, "AlertDispatchRequests");
    }

    private static void AddContextColumns(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.AddColumn<string>(name: "RelatedEntityType", table: table, type: "character varying(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "RelatedEntityId", table: table, type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "RelatedEntityCode", table: table, type: "character varying(128)", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<string>(name: "RelatedEntityName", table: table, type: "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<string>(name: "RelatedEntityStatus", table: table, type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>(name: "RelatedEntityPriority", table: table, type: "character varying(32)", maxLength: 32, nullable: true);
    }

    private static void DropContextColumns(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.DropColumn(name: "RelatedEntityPriority", table: table);
        migrationBuilder.DropColumn(name: "RelatedEntityStatus", table: table);
        migrationBuilder.DropColumn(name: "RelatedEntityName", table: table);
        migrationBuilder.DropColumn(name: "RelatedEntityCode", table: table);
        migrationBuilder.DropColumn(name: "RelatedEntityId", table: table);
        migrationBuilder.DropColumn(name: "RelatedEntityType", table: table);
    }
}
