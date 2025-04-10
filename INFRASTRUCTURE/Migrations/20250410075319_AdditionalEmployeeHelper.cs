using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalEmployeeHelper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationBackgrounds");

            migrationBuilder.DropTable(
                name: "FamilyMembers");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact_ContactNumber",
                table: "Employees",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact_FullName",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact_Relationship",
                table: "Employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact_ResidentialAddress",
                table: "Employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Father_FullName",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Father_LifeStatus",
                table: "Employees",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Father_Occupation",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Father_PhoneNumber",
                table: "Employees",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mother_FullName",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Mother_LifeStatus",
                table: "Employees",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mother_Occupation",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mother_PhoneNumber",
                table: "Employees",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextOfKin_ContactNumber",
                table: "Employees",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextOfKin_FullName",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextOfKin_Relationship",
                table: "Employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextOfKin_ResidentialAddress",
                table: "Employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Spouse_FullName",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Spouse_LifeStatus",
                table: "Employees",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Spouse_Occupation",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Spouse_PhoneNumber",
                table: "Employees",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Children",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Children_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Education",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Major = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QualificationEarned = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Education", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Education_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Children_EmployeeId",
                table: "Children",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Education_EmployeeId",
                table: "Education",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Children");

            migrationBuilder.DropTable(
                name: "Education");

            migrationBuilder.DropColumn(
                name: "EmergencyContact_ContactNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContact_FullName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContact_Relationship",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContact_ResidentialAddress",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Father_FullName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Father_LifeStatus",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Father_Occupation",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Father_PhoneNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Mother_FullName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Mother_LifeStatus",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Mother_Occupation",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Mother_PhoneNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NextOfKin_ContactNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NextOfKin_FullName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NextOfKin_Relationship",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NextOfKin_ResidentialAddress",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Spouse_FullName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Spouse_LifeStatus",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Spouse_Occupation",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Spouse_PhoneNumber",
                table: "Employees");

            migrationBuilder.CreateTable(
                name: "EducationBackgrounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Major = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QualificationEarned = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SchoolName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationBackgrounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationBackgrounds_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LifeStatus = table.Column<int>(type: "integer", nullable: false),
                    Occupation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Relationship = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ResidentialAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationBackgrounds_EmployeeId",
                table: "EducationBackgrounds",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_EmployeeId",
                table: "FamilyMembers",
                column: "EmployeeId");
        }
    }
}
