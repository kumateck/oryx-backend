using System;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261025090000_AddShipmentReferenceToNotifications")]
public partial class AddShipmentReferenceToNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "ShipmentDocumentId", table: "AlertDispatchRequests", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "ShipmentDocumentId", table: "Notifications", type: "uuid", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ShipmentDocumentId", table: "Notifications");
        migrationBuilder.DropColumn(name: "ShipmentDocumentId", table: "AlertDispatchRequests");
    }
}
