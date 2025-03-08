using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    Avatar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "roleclaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roleclaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roleclaims_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Approvals_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Approvals_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Approvals_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attachments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attachments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Prefix = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    NamingType = table.Column<int>(type: "integer", nullable: false),
                    MinimumNameLength = table.Column<int>(type: "integer", nullable: false),
                    MaximumNameLength = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Configurations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Configurations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Configurations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Countries_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Countries_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Countries_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Symbol = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Currencies_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Currencies_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Currencies_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Forms_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Forms_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Forms_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Grns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierName = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    VehicleNumber = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    GrnNumber = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grns_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Grns_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Grns_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MaterialKind = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialCategories_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialCategories_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialCategories_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialTypes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialTypes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialTypes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Operations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Operations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Operations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Organizations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Organizations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PackageTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageTypes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PackageTypes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PackageTypes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PasswordResets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    KeyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResets_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PasswordResets_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PasswordResets_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PasswordResets_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategories_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductCategories_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductCategories_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ScheduledStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionSchedules_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionSchedules_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionSchedules_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Validation = table.Column<int>(type: "integer", nullable: false),
                    IsMultiSelect = table.Column<bool>(type: "boolean", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Questions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Questions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentDiscrepancyTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentDiscrepancyTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyTypes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyTypes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyTypes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sites_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sites_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sites_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UnitOfMeasures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsScalable = table.Column<bool>(type: "boolean", nullable: false),
                    IsRawMaterial = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOfMeasures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitOfMeasures_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UnitOfMeasures_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UnitOfMeasures_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "userclaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userclaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_userclaims_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userlogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userlogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_userlogins_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userroles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userroles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_userroles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_userroles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usertokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usertokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_usertokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    MaterialKind = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Warehouses_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Warehouses_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Warehouses_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCenters_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkCenters_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkCenters_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalStages_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalStages_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalStages_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalStages_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalStages_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalStages_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Manufacturers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manufacturers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Manufacturers_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Manufacturers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Manufacturers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Manufacturers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContactNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Suppliers_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Suppliers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Suppliers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Suppliers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormAssignees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormAssignees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormAssignees_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormAssignees_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormReviewers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormReviewers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormReviewers_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormReviewers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSections_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormSections_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormSections_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormSections_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Responses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Responses_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Responses_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Responses_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Responses_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Pharmacopoeia = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Alphabet = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    MaterialCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    MinimumStockLevel = table.Column<int>(type: "integer", nullable: false),
                    MaximumStockLevel = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Materials_MaterialCategories_MaterialCategoryId",
                        column: x => x.MaterialCategoryId,
                        principalTable: "MaterialCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Materials_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Materials_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Materials_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionOptions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuestionOptions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuestionOptions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MachineId = table.Column<string>(type: "text", nullable: true),
                    IsStorage = table.Column<bool>(type: "boolean", nullable: false),
                    CapacityQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelevanceCheck = table.Column<bool>(type: "boolean", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageLocation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipments_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Equipments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Equipments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WarehouseArrivalLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FloorName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseArrivalLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseArrivalLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseArrivalLocations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseArrivalLocations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseArrivalLocations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FloorName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoices_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentInvoices_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentInvoices_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentInvoices_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SourceRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentQuotationRequestAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceRequisitions_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Response = table.Column<string>(type: "character varying(1000000)", maxLength: 1000000, nullable: true),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormFields_FormSections_FormSectionId",
                        column: x => x.FormSectionId,
                        principalTable: "FormSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormFields_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormFields_users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFields_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFields_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFields_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormFields_users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ManufacturerMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManufacturerMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManufacturerMaterials_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManufacturerMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManufacturerMaterials_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManufacturerMaterials_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManufacturerMaterials_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionScheduleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionScheduleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItems_ProductionSchedules_ProductionSched~",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItems_UnitOfMeasures_UomId",
                        column: x => x.UomId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionScheduleItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplierManufacturers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuantityPerPack = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierManufacturers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierManufacturers_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierManufacturers_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierManufacturers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierManufacturers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierManufacturers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierManufacturers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    GenericName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StorageCondition = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PackageStyle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FilledWeight = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShelfLife = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ActionUse = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FdaRegistrationNumber = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MasterFormulaNumber = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PrimaryPackDescription = table.Column<string>(type: "character varying(1000000)", maxLength: 1000000, nullable: true),
                    SecondaryPackDescription = table.Column<string>(type: "character varying(1000000)", maxLength: 1000000, nullable: true),
                    TertiaryPackDescription = table.Column<string>(type: "character varying(1000000)", maxLength: 1000000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    BasePackingQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseUomId = table.Column<Guid>(type: "uuid", nullable: true),
                    BasePackingUomId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullBatchSize = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Products_UnitOfMeasures_BasePackingUomId",
                        column: x => x.BasePackingUomId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_UnitOfMeasures_BaseUomId",
                        column: x => x.BaseUomId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocationRacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocationRacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseLocationRacks_WarehouseLocations_WarehouseLocation~",
                        column: x => x.WarehouseLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseLocationRacks_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseLocationRacks_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseLocationRacks_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShipmentInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedDistributionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDocuments_ShipmentInvoices_ShipmentInvoiceId",
                        column: x => x.ShipmentInvoiceId,
                        principalTable: "ShipmentInvoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDocuments_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDocuments_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDocuments_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_SourceRequisitions_SourceRequisitionId",
                        column: x => x.SourceRequisitionId,
                        principalTable: "SourceRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SourceRequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceRequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_SourceRequisitions_SourceRequisition~",
                        column: x => x.SourceRequisitionId,
                        principalTable: "SourceRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SourceRequisitionItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedQuotation = table.Column<bool>(type: "boolean", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_SourceRequisitions_SourceRequisitionId",
                        column: x => x.SourceRequisitionId,
                        principalTable: "SourceRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: true),
                    FormId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormResponses_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormResponses_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormResponses_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormResponses_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormResponses_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormResponses_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BillOfMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillOfMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinishedProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardCost = table.Column<decimal>(type: "numeric", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    DosageForm = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Strength = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedProducts_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinishedProducts_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProducts_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinishedProducts_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MasterProductionSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterProductionSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterProductionSchedules_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MasterProductionSchedules_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MasterProductionSchedules_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MasterProductionSchedules_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivities_ProductionSchedules_ProductionSchedule~",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivities_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivities_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivities_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivities_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionScheduleProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionScheduleProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleProducts_ProductionSchedules_ProductionSc~",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionScheduleProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialThickness = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OtherStandards = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BaseQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseUoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitCapacity = table.Column<decimal>(type: "numeric", nullable: false),
                    DirectLinkMaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    PackingExcessMargin = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPackages_Materials_DirectLinkMaterialId",
                        column: x => x.DirectLinkMaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductPackages_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductPackages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductPackages_UnitOfMeasures_BaseUoMId",
                        column: x => x.BaseUoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductPackages_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductPackages_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductPackages_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedTime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_Forms_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Forms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Routes_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Routes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Routes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Routes_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Routes_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocationShelves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseLocationRackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocationShelves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseLocationShelves_WarehouseLocationRacks_WarehouseLo~",
                        column: x => x.WarehouseLocationRackId,
                        principalTable: "WarehouseLocationRacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseLocationShelves_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseLocationShelves_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseLocationShelves_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentDiscrepancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentDiscrepancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancies_ShipmentDocuments_ShipmentDocumentId",
                        column: x => x.ShipmentDocumentId,
                        principalTable: "ShipmentDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancies_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancies_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancies_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderInvoices_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderInvoices_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderInvoices_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderInvoices_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItem_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItem_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItem_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItem_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RevisedPurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisedPurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrders_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrders_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrders_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrders_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Distributed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItems_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItems_ShipmentInvoices_ShipmentInvoiceId",
                        column: x => x.ShipmentInvoiceId,
                        principalTable: "ShipmentInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentInvoiceItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuotationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    QuotedPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuotationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_SupplierQuotations_SupplierQuotation~",
                        column: x => x.SupplierQuotationId,
                        principalTable: "SupplierQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierQuotationItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BillOfMaterialItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Grade = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CasNumber = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Function = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsSubstitutable = table.Column<bool>(type: "boolean", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseUoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillOfMaterialItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItems_BillOfMaterials_BillOfMaterialId",
                        column: x => x.BillOfMaterialId,
                        principalTable: "BillOfMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItems_MaterialTypes_MaterialTypeId",
                        column: x => x.MaterialTypeId,
                        principalTable: "MaterialTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItems_UnitOfMeasures_BaseUoMId",
                        column: x => x.BaseUoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillOfMaterialItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductBillOfMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillOfMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBillOfMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBillOfMaterials_BillOfMaterials_BillOfMaterialId",
                        column: x => x.BillOfMaterialId,
                        principalTable: "BillOfMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductBillOfMaterials_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductBillOfMaterials_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBillOfMaterials_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBillOfMaterials_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MasterProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrders_MasterProductionSchedules_MasterProductionSchedu~",
                        column: x => x.MasterProductionScheduleId,
                        principalTable: "MasterProductionSchedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrders_ProductionSchedules_ProductionScheduleId",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrders_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrders_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrders_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivityLogs_ProductionActivities_ProductionActiv~",
                        column: x => x.ProductionActivityId,
                        principalTable: "ProductionActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityLogs_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityLogs_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityLogs_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityLogs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionActivitySteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivitySteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_Forms_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Forms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_ProductionActivities_ProductionActi~",
                        column: x => x.ProductionActivityId,
                        principalTable: "ProductionActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivitySteps_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RouteResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteResources_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResources_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResources_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResources_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResources_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RouteResponsibleRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteResponsibleRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleRoles_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleRoles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleRoles_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleRoles_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleRoles_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RouteResponsibleUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteResponsibleUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleUsers_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteResponsibleUsers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleUsers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleUsers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteResponsibleUsers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteWorkCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteWorkCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteWorkCenters_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteWorkCenters_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteWorkCenters_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteWorkCenters_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteWorkCenters_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentDiscrepancyItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentDiscrepancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    TypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Resolved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentDiscrepancyItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_ShipmentDiscrepancies_ShipmentDiscr~",
                        column: x => x.ShipmentDiscrepancyId,
                        principalTable: "ShipmentDiscrepancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_ShipmentDiscrepancyTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "ShipmentDiscrepancyTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentDiscrepancyItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BatchItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PurchaseOrderInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchItem_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchItem_PurchaseOrderInvoices_PurchaseOrderInvoiceId",
                        column: x => x.PurchaseOrderInvoiceId,
                        principalTable: "PurchaseOrderInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BillingSheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BillOfLading = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedArrivalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FreeTimeExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FreeTimeDuration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    DemurrageStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContainerNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NumberOfPackages = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PackageDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingSheets_PurchaseOrderInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "PurchaseOrderInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingSheets_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingSheets_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillingSheets_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BillingSheets_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Charge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Charge_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Charge_PurchaseOrderInvoices_PurchaseOrderInvoiceId",
                        column: x => x.PurchaseOrderInvoiceId,
                        principalTable: "PurchaseOrderInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Charge_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Charge_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Charge_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RevisedPurchaseOrderItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisedPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisedPurchaseOrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_RevisedPurchaseOrders_RevisedPurch~",
                        column: x => x.RevisedPurchaseOrderId,
                        principalTable: "RevisedPurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RevisedPurchaseOrderItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionSteps_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionSteps_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionSteps_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionSteps_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionSteps_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BatchManufacturingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BatchQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchManufacturingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_ProductionActivitySteps_Productio~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_ProductionSchedules_ProductionSch~",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchManufacturingRecords_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BatchPackagingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BatchQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchPackagingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_ProductionActivitySteps_ProductionAct~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_ProductionSchedules_ProductionSchedul~",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BatchPackagingRecords_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionActivityStepResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivityStepResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepResources_ProductionActivitySteps_Pro~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepResources_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepResources_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepResources_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepResources_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionActivityStepUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivityStepUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepUsers_ProductionActivitySteps_Product~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepUsers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepUsers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepUsers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepUsers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionActivityStepWorkCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionActivityStepWorkCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepWorkCenters_ProductionActivitySteps_P~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepWorkCenters_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepWorkCenters_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepWorkCenters_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionActivityStepWorkCenters_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Requisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequisitionType = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    ExpectedDelivery = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requisitions_ProductionActivitySteps_ProductionActivityStep~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Requisitions_ProductionSchedules_ProductionScheduleId",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Requisitions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Requisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Requisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Requisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Requisitions_users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequiredQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionActivityStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransfers_ProductionActivitySteps_ProductionActivitySt~",
                        column: x => x.ProductionActivityStepId,
                        principalTable: "ProductionActivitySteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_ProductionSchedules_ProductionScheduleId",
                        column: x => x.ProductionScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfers_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CompletedRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequisitionType = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequisitionApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionApprovals_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequisitionApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequisitionApprovals_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequisitionApprovals_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequisitionApprovals_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequisitionApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequisitionItems_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequisitionItems_UnitOfMeasures_UomId",
                        column: x => x.UomId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequisitionItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequisitionItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequisitionItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockTransferSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockTransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromDepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToDepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferSources_Departments_FromDepartmentId",
                        column: x => x.FromDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransferSources_Departments_ToDepartmentId",
                        column: x => x.ToDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransferSources_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransferSources_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransferSources_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransferSources_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransferSources_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CompletedRequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedRequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_CompletedRequisitions_CompletedRe~",
                        column: x => x.CompletedRequisitionId,
                        principalTable: "CompletedRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompletedRequisitionItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DistributedRequisitionMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseArrivalLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShipmentInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    UomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    DistributedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrnGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributedRequisitionMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_RequisitionItems_Requisitio~",
                        column: x => x.RequisitionItemId,
                        principalTable: "RequisitionItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_ShipmentInvoices_ShipmentIn~",
                        column: x => x.ShipmentInvoiceId,
                        principalTable: "ShipmentInvoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_UnitOfMeasures_UomId",
                        column: x => x.UomId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_WarehouseArrivalLocations_W~",
                        column: x => x.WarehouseArrivalLocationId,
                        principalTable: "WarehouseArrivalLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DistributedRequisitionMaterials_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Checklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistributedRequisitionMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShipmentInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CertificateOfAnalysisDelivered = table.Column<bool>(type: "boolean", nullable: false),
                    VisibleLabelling = table.Column<bool>(type: "boolean", nullable: false),
                    IntactnessStatus = table.Column<int>(type: "integer", nullable: false),
                    ConsignmentCarrierStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Checklists_DistributedRequisitionMaterials_DistributedRequi~",
                        column: x => x.DistributedRequisitionMaterialId,
                        principalTable: "DistributedRequisitionMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Checklists_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Checklists_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Checklists_ShipmentInvoices_ShipmentInvoiceId",
                        column: x => x.ShipmentInvoiceId,
                        principalTable: "ShipmentInvoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Checklists_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Checklists_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Checklists_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Checklists_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialItemDistributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistributedRequisitionMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentInvoiceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialItemDistributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialItemDistributions_DistributedRequisitionMaterials_D~",
                        column: x => x.DistributedRequisitionMaterialId,
                        principalTable: "DistributedRequisitionMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialItemDistributions_ShipmentInvoiceItems_ShipmentInvo~",
                        column: x => x.ShipmentInvoiceItemId,
                        principalTable: "ShipmentInvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChecklistId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    GrnId = table.Column<Guid>(type: "uuid", nullable: true),
                    NumberOfContainers = table.Column<int>(type: "integer", nullable: false),
                    ContainerUoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuantityPerContainer = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityAssigned = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DateReceived = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateApproved = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateRejected = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialBatches_Checklists_ChecklistId",
                        column: x => x.ChecklistId,
                        principalTable: "Checklists",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatches_Grns_GrnId",
                        column: x => x.GrnId,
                        principalTable: "Grns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatches_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialBatches_UnitOfMeasures_ContainerUoMId",
                        column: x => x.ContainerUoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatches_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatches_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatches_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatches_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BinCardInformation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WayBill = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ArNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuantityReceived = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityIssued = table.Column<decimal>(type: "numeric", nullable: false),
                    BalanceQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinCardInformation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinCardInformation_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BinCardInformation_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BinCardInformation_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BinCardInformation_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BinCardInformation_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BinCardInformation_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MassMaterialBatchMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    MovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MovedById = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MassMaterialBatchMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_MaterialBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassMaterialBatchMovements_users_MovedById",
                        column: x => x.MovedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialBatchEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ConsumptionWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialBatchEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialBatchEvents_MaterialBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialBatchEvents_Warehouses_ConsumptionWarehouseId",
                        column: x => x.ConsumptionWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatchEvents_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatchEvents_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatchEvents_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialBatchEvents_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShelfMaterialBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseLocationShelfId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShelfMaterialBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatches_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatches_UnitOfMeasures_UomId",
                        column: x => x.UomId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatches_WarehouseLocationShelves_WarehouseLoca~",
                        column: x => x.WarehouseLocationShelfId,
                        principalTable: "WarehouseLocationShelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatches_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatches_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShelfMaterialBatches_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Srs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SrNumber = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    GrossWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Srs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Srs_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Srs_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Srs_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Srs_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Srs_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_CreatedById",
                table: "Approvals",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_LastDeletedById",
                table: "Approvals",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_LastUpdatedById",
                table: "Approvals",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStages_ApprovalId",
                table: "ApprovalStages",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStages_CreatedById",
                table: "ApprovalStages",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStages_LastDeletedById",
                table: "ApprovalStages",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStages_LastUpdatedById",
                table: "ApprovalStages",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStages_RoleId",
                table: "ApprovalStages",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStages_UserId",
                table: "ApprovalStages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CreatedById",
                table: "Attachments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_LastDeletedById",
                table: "Attachments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_LastUpdatedById",
                table: "Attachments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchItem_CreatedById",
                table: "BatchItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchItem_LastDeletedById",
                table: "BatchItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchItem_LastUpdatedById",
                table: "BatchItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchItem_ManufacturerId",
                table: "BatchItem",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchItem_PurchaseOrderInvoiceId",
                table: "BatchItem",
                column: "PurchaseOrderInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_CreatedById",
                table: "BatchManufacturingRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_IssuedById",
                table: "BatchManufacturingRecords",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_LastDeletedById",
                table: "BatchManufacturingRecords",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_LastUpdatedById",
                table: "BatchManufacturingRecords",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_ProductId",
                table: "BatchManufacturingRecords",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_ProductionActivityStepId",
                table: "BatchManufacturingRecords",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchManufacturingRecords_ProductionScheduleId",
                table: "BatchManufacturingRecords",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_CreatedById",
                table: "BatchPackagingRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_IssuedById",
                table: "BatchPackagingRecords",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_LastDeletedById",
                table: "BatchPackagingRecords",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_LastUpdatedById",
                table: "BatchPackagingRecords",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_ProductId",
                table: "BatchPackagingRecords",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_ProductionActivityStepId",
                table: "BatchPackagingRecords",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPackagingRecords_ProductionScheduleId",
                table: "BatchPackagingRecords",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheets_CreatedById",
                table: "BillingSheets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheets_InvoiceId",
                table: "BillingSheets",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheets_LastDeletedById",
                table: "BillingSheets",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheets_LastUpdatedById",
                table: "BillingSheets",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSheets_SupplierId",
                table: "BillingSheets",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_BaseUoMId",
                table: "BillOfMaterialItems",
                column: "BaseUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_BillOfMaterialId",
                table: "BillOfMaterialItems",
                column: "BillOfMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_CreatedById",
                table: "BillOfMaterialItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_LastDeletedById",
                table: "BillOfMaterialItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_LastUpdatedById",
                table: "BillOfMaterialItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_MaterialId",
                table: "BillOfMaterialItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_MaterialTypeId",
                table: "BillOfMaterialItems",
                column: "MaterialTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_CreatedById",
                table: "BillOfMaterials",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_LastDeletedById",
                table: "BillOfMaterials",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_LastUpdatedById",
                table: "BillOfMaterials",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_ProductId",
                table: "BillOfMaterials",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BinCardInformation_CreatedById",
                table: "BinCardInformation",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BinCardInformation_LastDeletedById",
                table: "BinCardInformation",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BinCardInformation_LastUpdatedById",
                table: "BinCardInformation",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BinCardInformation_MaterialBatchId",
                table: "BinCardInformation",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BinCardInformation_ProductId",
                table: "BinCardInformation",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BinCardInformation_UoMId",
                table: "BinCardInformation",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_Charge_CreatedById",
                table: "Charge",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Charge_CurrencyId",
                table: "Charge",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Charge_LastDeletedById",
                table: "Charge",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Charge_LastUpdatedById",
                table: "Charge",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Charge_PurchaseOrderInvoiceId",
                table: "Charge",
                column: "PurchaseOrderInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_CreatedById",
                table: "Checklists",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_DistributedRequisitionMaterialId",
                table: "Checklists",
                column: "DistributedRequisitionMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_LastDeletedById",
                table: "Checklists",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_LastUpdatedById",
                table: "Checklists",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_ManufacturerId",
                table: "Checklists",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_MaterialId",
                table: "Checklists",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_ShipmentInvoiceId",
                table: "Checklists",
                column: "ShipmentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_SupplierId",
                table: "Checklists",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_CompletedRequisitionId",
                table: "CompletedRequisitionItems",
                column: "CompletedRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_CreatedById",
                table: "CompletedRequisitionItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_LastDeletedById",
                table: "CompletedRequisitionItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_LastUpdatedById",
                table: "CompletedRequisitionItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_MaterialId",
                table: "CompletedRequisitionItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitionItems_UoMId",
                table: "CompletedRequisitionItems",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_CreatedById",
                table: "CompletedRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_LastDeletedById",
                table: "CompletedRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_LastUpdatedById",
                table: "CompletedRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedRequisitions_RequisitionId",
                table: "CompletedRequisitions",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_CreatedById",
                table: "Configurations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_LastDeletedById",
                table: "Configurations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_LastUpdatedById",
                table: "Configurations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_CreatedById",
                table: "Countries",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_LastDeletedById",
                table: "Countries",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_LastUpdatedById",
                table: "Countries",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_CreatedById",
                table: "Currencies",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_LastDeletedById",
                table: "Currencies",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_LastUpdatedById",
                table: "Currencies",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_CreatedById",
                table: "DistributedRequisitionMaterials",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_LastDeletedById",
                table: "DistributedRequisitionMaterials",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_LastUpdatedById",
                table: "DistributedRequisitionMaterials",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_MaterialId",
                table: "DistributedRequisitionMaterials",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_RequisitionItemId",
                table: "DistributedRequisitionMaterials",
                column: "RequisitionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_ShipmentInvoiceId",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_UomId",
                table: "DistributedRequisitionMaterials",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_WarehouseArrivalLocationId",
                table: "DistributedRequisitionMaterials",
                column: "WarehouseArrivalLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_CreatedById",
                table: "Equipments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_DepartmentId",
                table: "Equipments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_LastDeletedById",
                table: "Equipments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_LastUpdatedById",
                table: "Equipments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_UoMId",
                table: "Equipments",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProducts_CreatedById",
                table: "FinishedProducts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProducts_LastDeletedById",
                table: "FinishedProducts",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProducts_LastUpdatedById",
                table: "FinishedProducts",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProducts_ProductId",
                table: "FinishedProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProducts_UoMId",
                table: "FinishedProducts",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_FormId",
                table: "FormAssignees",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAssignees_UserId",
                table: "FormAssignees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_AssigneeId",
                table: "FormFields",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_CreatedById",
                table: "FormFields",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FormSectionId",
                table: "FormFields",
                column: "FormSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_LastDeletedById",
                table: "FormFields",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_LastUpdatedById",
                table: "FormFields",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_QuestionId",
                table: "FormFields",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_ReviewerId",
                table: "FormFields",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_CreatedById",
                table: "FormResponses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_FormFieldId",
                table: "FormResponses",
                column: "FormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_FormId",
                table: "FormResponses",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_LastDeletedById",
                table: "FormResponses",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_LastUpdatedById",
                table: "FormResponses",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_ResponseId",
                table: "FormResponses",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_FormReviewers_FormId",
                table: "FormReviewers",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormReviewers_UserId",
                table: "FormReviewers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_CreatedById",
                table: "Forms",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_LastDeletedById",
                table: "Forms",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_LastUpdatedById",
                table: "Forms",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_CreatedById",
                table: "FormSections",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_FormId",
                table: "FormSections",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_LastDeletedById",
                table: "FormSections",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_LastUpdatedById",
                table: "FormSections",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Grns_CreatedById",
                table: "Grns",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Grns_LastDeletedById",
                table: "Grns",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Grns_LastUpdatedById",
                table: "Grns",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturerMaterials_CreatedById",
                table: "ManufacturerMaterials",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturerMaterials_LastDeletedById",
                table: "ManufacturerMaterials",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturerMaterials_LastUpdatedById",
                table: "ManufacturerMaterials",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturerMaterials_ManufacturerId",
                table: "ManufacturerMaterials",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturerMaterials_MaterialId",
                table: "ManufacturerMaterials",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_CountryId",
                table: "Manufacturers",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_CreatedById",
                table: "Manufacturers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_LastDeletedById",
                table: "Manufacturers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_LastUpdatedById",
                table: "Manufacturers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_BatchId",
                table: "MassMaterialBatchMovements",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_CreatedById",
                table: "MassMaterialBatchMovements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_FromWarehouseId",
                table: "MassMaterialBatchMovements",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_LastDeletedById",
                table: "MassMaterialBatchMovements",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_LastUpdatedById",
                table: "MassMaterialBatchMovements",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_MovedById",
                table: "MassMaterialBatchMovements",
                column: "MovedById");

            migrationBuilder.CreateIndex(
                name: "IX_MassMaterialBatchMovements_ToWarehouseId",
                table: "MassMaterialBatchMovements",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProductionSchedules_CreatedById",
                table: "MasterProductionSchedules",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProductionSchedules_LastDeletedById",
                table: "MasterProductionSchedules",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProductionSchedules_LastUpdatedById",
                table: "MasterProductionSchedules",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProductionSchedules_ProductId",
                table: "MasterProductionSchedules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_ChecklistId",
                table: "MaterialBatches",
                column: "ChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_ContainerUoMId",
                table: "MaterialBatches",
                column: "ContainerUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_CreatedById",
                table: "MaterialBatches",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_GrnId",
                table: "MaterialBatches",
                column: "GrnId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_LastDeletedById",
                table: "MaterialBatches",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_LastUpdatedById",
                table: "MaterialBatches",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_MaterialId",
                table: "MaterialBatches",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_UoMId",
                table: "MaterialBatches",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchEvents_BatchId",
                table: "MaterialBatchEvents",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchEvents_ConsumptionWarehouseId",
                table: "MaterialBatchEvents",
                column: "ConsumptionWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchEvents_CreatedById",
                table: "MaterialBatchEvents",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchEvents_LastDeletedById",
                table: "MaterialBatchEvents",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchEvents_LastUpdatedById",
                table: "MaterialBatchEvents",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatchEvents_UserId",
                table: "MaterialBatchEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCategories_CreatedById",
                table: "MaterialCategories",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCategories_LastDeletedById",
                table: "MaterialCategories",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCategories_LastUpdatedById",
                table: "MaterialCategories",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialItemDistributions_DistributedRequisitionMaterialId",
                table: "MaterialItemDistributions",
                column: "DistributedRequisitionMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialItemDistributions_ShipmentInvoiceItemId",
                table: "MaterialItemDistributions",
                column: "ShipmentInvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_CreatedById",
                table: "Materials",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_LastDeletedById",
                table: "Materials",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_LastUpdatedById",
                table: "Materials",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_MaterialCategoryId",
                table: "Materials",
                column: "MaterialCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTypes_CreatedById",
                table: "MaterialTypes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTypes_LastDeletedById",
                table: "MaterialTypes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTypes_LastUpdatedById",
                table: "MaterialTypes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_CreatedById",
                table: "Operations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_LastDeletedById",
                table: "Operations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_LastUpdatedById",
                table: "Operations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CreatedById",
                table: "Organizations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_LastDeletedById",
                table: "Organizations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_LastUpdatedById",
                table: "Organizations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PackageTypes_CreatedById",
                table: "PackageTypes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PackageTypes_LastDeletedById",
                table: "PackageTypes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PackageTypes_LastUpdatedById",
                table: "PackageTypes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_CreatedById",
                table: "PasswordResets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_LastDeletedById",
                table: "PasswordResets",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_LastUpdatedById",
                table: "PasswordResets",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_UserId",
                table: "PasswordResets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBillOfMaterials_BillOfMaterialId",
                table: "ProductBillOfMaterials",
                column: "BillOfMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBillOfMaterials_CreatedById",
                table: "ProductBillOfMaterials",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBillOfMaterials_LastDeletedById",
                table: "ProductBillOfMaterials",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBillOfMaterials_LastUpdatedById",
                table: "ProductBillOfMaterials",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBillOfMaterials_ProductId",
                table: "ProductBillOfMaterials",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_CreatedById",
                table: "ProductCategories",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_LastDeletedById",
                table: "ProductCategories",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_LastUpdatedById",
                table: "ProductCategories",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_CreatedById",
                table: "ProductionActivities",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_LastDeletedById",
                table: "ProductionActivities",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_LastUpdatedById",
                table: "ProductionActivities",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_ProductId",
                table: "ProductionActivities",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivities_ProductionScheduleId",
                table: "ProductionActivities",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityLogs_CreatedById",
                table: "ProductionActivityLogs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityLogs_LastDeletedById",
                table: "ProductionActivityLogs",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityLogs_LastUpdatedById",
                table: "ProductionActivityLogs",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityLogs_ProductionActivityId",
                table: "ProductionActivityLogs",
                column: "ProductionActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityLogs_UserId",
                table: "ProductionActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepResources_CreatedById",
                table: "ProductionActivityStepResources",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepResources_LastDeletedById",
                table: "ProductionActivityStepResources",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepResources_LastUpdatedById",
                table: "ProductionActivityStepResources",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepResources_ProductionActivityStepId",
                table: "ProductionActivityStepResources",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepResources_ResourceId",
                table: "ProductionActivityStepResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_CreatedById",
                table: "ProductionActivitySteps",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_LastDeletedById",
                table: "ProductionActivitySteps",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_LastUpdatedById",
                table: "ProductionActivitySteps",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_OperationId",
                table: "ProductionActivitySteps",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_ProductionActivityId",
                table: "ProductionActivitySteps",
                column: "ProductionActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivitySteps_WorkflowId",
                table: "ProductionActivitySteps",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_CreatedById",
                table: "ProductionActivityStepUsers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_LastDeletedById",
                table: "ProductionActivityStepUsers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_LastUpdatedById",
                table: "ProductionActivityStepUsers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_ProductionActivityStepId",
                table: "ProductionActivityStepUsers",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepUsers_UserId",
                table: "ProductionActivityStepUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepWorkCenters_CreatedById",
                table: "ProductionActivityStepWorkCenters",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepWorkCenters_LastDeletedById",
                table: "ProductionActivityStepWorkCenters",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepWorkCenters_LastUpdatedById",
                table: "ProductionActivityStepWorkCenters",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepWorkCenters_ProductionActivityStepId",
                table: "ProductionActivityStepWorkCenters",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionActivityStepWorkCenters_WorkCenterId",
                table: "ProductionActivityStepWorkCenters",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItems_CreatedById",
                table: "ProductionScheduleItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItems_LastDeletedById",
                table: "ProductionScheduleItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItems_LastUpdatedById",
                table: "ProductionScheduleItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItems_MaterialId",
                table: "ProductionScheduleItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItems_ProductionScheduleId",
                table: "ProductionScheduleItems",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleItems_UomId",
                table: "ProductionScheduleItems",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleProducts_ProductId",
                table: "ProductionScheduleProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionScheduleProducts_ProductionScheduleId",
                table: "ProductionScheduleProducts",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSchedules_CreatedById",
                table: "ProductionSchedules",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSchedules_LastDeletedById",
                table: "ProductionSchedules",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSchedules_LastUpdatedById",
                table: "ProductionSchedules",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSteps_CreatedById",
                table: "ProductionSteps",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSteps_LastDeletedById",
                table: "ProductionSteps",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSteps_LastUpdatedById",
                table: "ProductionSteps",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSteps_ResourceId",
                table: "ProductionSteps",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSteps_WorkOrderId",
                table: "ProductionSteps",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_BaseUoMId",
                table: "ProductPackages",
                column: "BaseUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_CreatedById",
                table: "ProductPackages",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_DirectLinkMaterialId",
                table: "ProductPackages",
                column: "DirectLinkMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_LastDeletedById",
                table: "ProductPackages",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_LastUpdatedById",
                table: "ProductPackages",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_MaterialId",
                table: "ProductPackages",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_ProductId",
                table: "ProductPackages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BasePackingUomId",
                table: "Products",
                column: "BasePackingUomId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BaseUomId",
                table: "Products",
                column: "BaseUomId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedById",
                table: "Products",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Products_DepartmentId",
                table: "Products",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_EquipmentId",
                table: "Products",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_LastDeletedById",
                table: "Products",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Products_LastUpdatedById",
                table: "Products",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderInvoices_CreatedById",
                table: "PurchaseOrderInvoices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderInvoices_LastDeletedById",
                table: "PurchaseOrderInvoices",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderInvoices_LastUpdatedById",
                table: "PurchaseOrderInvoices",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderInvoices_PurchaseOrderId",
                table: "PurchaseOrderInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItem_CreatedById",
                table: "PurchaseOrderItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItem_CurrencyId",
                table: "PurchaseOrderItem",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItem_LastDeletedById",
                table: "PurchaseOrderItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItem_LastUpdatedById",
                table: "PurchaseOrderItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItem_MaterialId",
                table: "PurchaseOrderItem",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItem_PurchaseOrderId",
                table: "PurchaseOrderItem",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItem_UoMId",
                table: "PurchaseOrderItem",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedById",
                table: "PurchaseOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_LastDeletedById",
                table: "PurchaseOrders",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_LastUpdatedById",
                table: "PurchaseOrders",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SourceRequisitionId",
                table: "PurchaseOrders",
                column: "SourceRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_CreatedById",
                table: "QuestionOptions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_LastDeletedById",
                table: "QuestionOptions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_LastUpdatedById",
                table: "QuestionOptions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_QuestionId",
                table: "QuestionOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CreatedById",
                table: "Questions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_LastDeletedById",
                table: "Questions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_LastUpdatedById",
                table: "Questions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_CreatedById",
                table: "RefreshTokens",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_LastDeletedById",
                table: "RefreshTokens",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_LastUpdatedById",
                table: "RefreshTokens",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_CreatedById",
                table: "RequisitionApprovals",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_LastDeletedById",
                table: "RequisitionApprovals",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_LastUpdatedById",
                table: "RequisitionApprovals",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_RequisitionId",
                table: "RequisitionApprovals",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_RoleId",
                table: "RequisitionApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_UserId",
                table: "RequisitionApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_CreatedById",
                table: "RequisitionItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_LastDeletedById",
                table: "RequisitionItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_LastUpdatedById",
                table: "RequisitionItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_MaterialId",
                table: "RequisitionItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_RequisitionId",
                table: "RequisitionItems",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_UomId",
                table: "RequisitionItems",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_CreatedById",
                table: "Requisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_LastDeletedById",
                table: "Requisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_LastUpdatedById",
                table: "Requisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_ProductId",
                table: "Requisitions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_ProductionActivityStepId",
                table: "Requisitions",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_ProductionScheduleId",
                table: "Requisitions",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_RequestedById",
                table: "Requisitions",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CreatedById",
                table: "Resources",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_LastDeletedById",
                table: "Resources",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_LastUpdatedById",
                table: "Resources",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Responses_CreatedById",
                table: "Responses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Responses_FormId",
                table: "Responses",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_Responses_LastDeletedById",
                table: "Responses",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Responses_LastUpdatedById",
                table: "Responses",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_CreatedById",
                table: "RevisedPurchaseOrderItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_CurrencyId",
                table: "RevisedPurchaseOrderItem",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_LastDeletedById",
                table: "RevisedPurchaseOrderItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_LastUpdatedById",
                table: "RevisedPurchaseOrderItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_MaterialId",
                table: "RevisedPurchaseOrderItem",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_RevisedPurchaseOrderId",
                table: "RevisedPurchaseOrderItem",
                column: "RevisedPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrderItem_UoMId",
                table: "RevisedPurchaseOrderItem",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrders_CreatedById",
                table: "RevisedPurchaseOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrders_LastDeletedById",
                table: "RevisedPurchaseOrders",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrders_LastUpdatedById",
                table: "RevisedPurchaseOrders",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrders_PurchaseOrderId",
                table: "RevisedPurchaseOrders",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_roleclaims_RoleId",
                table: "roleclaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteResources_CreatedById",
                table: "RouteResources",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResources_LastDeletedById",
                table: "RouteResources",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResources_LastUpdatedById",
                table: "RouteResources",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResources_ResourceId",
                table: "RouteResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResources_RouteId",
                table: "RouteResources",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_CreatedById",
                table: "RouteResponsibleRoles",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_LastDeletedById",
                table: "RouteResponsibleRoles",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_LastUpdatedById",
                table: "RouteResponsibleRoles",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_RoleId",
                table: "RouteResponsibleRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleRoles_RouteId",
                table: "RouteResponsibleRoles",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_CreatedById",
                table: "RouteResponsibleUsers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_LastDeletedById",
                table: "RouteResponsibleUsers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_LastUpdatedById",
                table: "RouteResponsibleUsers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_RouteId",
                table: "RouteResponsibleUsers",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteResponsibleUsers_UserId",
                table: "RouteResponsibleUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_CreatedById",
                table: "Routes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_LastDeletedById",
                table: "Routes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_LastUpdatedById",
                table: "Routes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_OperationId",
                table: "Routes",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ProductId",
                table: "Routes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_WorkflowId",
                table: "Routes",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWorkCenters_CreatedById",
                table: "RouteWorkCenters",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWorkCenters_LastDeletedById",
                table: "RouteWorkCenters",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWorkCenters_LastUpdatedById",
                table: "RouteWorkCenters",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWorkCenters_RouteId",
                table: "RouteWorkCenters",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWorkCenters_WorkCenterId",
                table: "RouteWorkCenters",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatches_CreatedById",
                table: "ShelfMaterialBatches",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatches_LastDeletedById",
                table: "ShelfMaterialBatches",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatches_LastUpdatedById",
                table: "ShelfMaterialBatches",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatches_MaterialBatchId",
                table: "ShelfMaterialBatches",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatches_UomId",
                table: "ShelfMaterialBatches",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBatches_WarehouseLocationShelfId",
                table: "ShelfMaterialBatches",
                column: "WarehouseLocationShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancies_CreatedById",
                table: "ShipmentDiscrepancies",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancies_LastDeletedById",
                table: "ShipmentDiscrepancies",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancies_LastUpdatedById",
                table: "ShipmentDiscrepancies",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancies_ShipmentDocumentId",
                table: "ShipmentDiscrepancies",
                column: "ShipmentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_CreatedById",
                table: "ShipmentDiscrepancyItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_LastDeletedById",
                table: "ShipmentDiscrepancyItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_LastUpdatedById",
                table: "ShipmentDiscrepancyItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_MaterialId",
                table: "ShipmentDiscrepancyItem",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_ShipmentDiscrepancyId",
                table: "ShipmentDiscrepancyItem",
                column: "ShipmentDiscrepancyId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_TypeId",
                table: "ShipmentDiscrepancyItem",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyItem_UoMId",
                table: "ShipmentDiscrepancyItem",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyTypes_CreatedById",
                table: "ShipmentDiscrepancyTypes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyTypes_LastDeletedById",
                table: "ShipmentDiscrepancyTypes",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDiscrepancyTypes_LastUpdatedById",
                table: "ShipmentDiscrepancyTypes",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_CreatedById",
                table: "ShipmentDocuments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_LastDeletedById",
                table: "ShipmentDocuments",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_LastUpdatedById",
                table: "ShipmentDocuments",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentDocuments_ShipmentInvoiceId",
                table: "ShipmentDocuments",
                column: "ShipmentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_CreatedById",
                table: "ShipmentInvoiceItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_LastDeletedById",
                table: "ShipmentInvoiceItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_LastUpdatedById",
                table: "ShipmentInvoiceItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_ManufacturerId",
                table: "ShipmentInvoiceItems",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_MaterialId",
                table: "ShipmentInvoiceItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_PurchaseOrderId",
                table: "ShipmentInvoiceItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_ShipmentInvoiceId",
                table: "ShipmentInvoiceItems",
                column: "ShipmentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_UoMId",
                table: "ShipmentInvoiceItems",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_CreatedById",
                table: "ShipmentInvoices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_LastDeletedById",
                table: "ShipmentInvoices",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_LastUpdatedById",
                table: "ShipmentInvoices",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoices_SupplierId",
                table: "ShipmentInvoices",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_CreatedById",
                table: "Sites",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_LastDeletedById",
                table: "Sites",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_LastUpdatedById",
                table: "Sites",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_CreatedById",
                table: "SourceRequisitionItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_LastDeletedById",
                table: "SourceRequisitionItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_LastUpdatedById",
                table: "SourceRequisitionItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_MaterialId",
                table: "SourceRequisitionItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_SourceRequisitionId",
                table: "SourceRequisitionItems",
                column: "SourceRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitionItems_UoMId",
                table: "SourceRequisitionItems",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitions_CreatedById",
                table: "SourceRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitions_LastDeletedById",
                table: "SourceRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitions_LastUpdatedById",
                table: "SourceRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRequisitions_SupplierId",
                table: "SourceRequisitions",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Srs_CreatedById",
                table: "Srs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Srs_LastDeletedById",
                table: "Srs",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Srs_LastUpdatedById",
                table: "Srs",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Srs_MaterialBatchId",
                table: "Srs",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Srs_UoMId",
                table: "Srs",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_CreatedById",
                table: "StockTransfers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_LastDeletedById",
                table: "StockTransfers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_LastUpdatedById",
                table: "StockTransfers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_MaterialId",
                table: "StockTransfers",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ProductId",
                table: "StockTransfers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ProductionActivityStepId",
                table: "StockTransfers",
                column: "ProductionActivityStepId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ProductionScheduleId",
                table: "StockTransfers",
                column: "ProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferSources_CreatedById",
                table: "StockTransferSources",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferSources_FromDepartmentId",
                table: "StockTransferSources",
                column: "FromDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferSources_LastDeletedById",
                table: "StockTransferSources",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferSources_LastUpdatedById",
                table: "StockTransferSources",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferSources_StockTransferId",
                table: "StockTransferSources",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferSources_ToDepartmentId",
                table: "StockTransferSources",
                column: "ToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferSources_UoMId",
                table: "StockTransferSources",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturers_CreatedById",
                table: "SupplierManufacturers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturers_LastDeletedById",
                table: "SupplierManufacturers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturers_LastUpdatedById",
                table: "SupplierManufacturers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturers_ManufacturerId",
                table: "SupplierManufacturers",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturers_MaterialId",
                table: "SupplierManufacturers",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierManufacturers_SupplierId",
                table: "SupplierManufacturers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_CreatedById",
                table: "SupplierQuotationItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_LastDeletedById",
                table: "SupplierQuotationItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_LastUpdatedById",
                table: "SupplierQuotationItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_MaterialId",
                table: "SupplierQuotationItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_SupplierQuotationId",
                table: "SupplierQuotationItems",
                column: "SupplierQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationItems_UoMId",
                table: "SupplierQuotationItems",
                column: "UoMId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_CreatedById",
                table: "SupplierQuotations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_LastDeletedById",
                table: "SupplierQuotations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_LastUpdatedById",
                table: "SupplierQuotations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_SourceRequisitionId",
                table: "SupplierQuotations",
                column: "SourceRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_SupplierId",
                table: "SupplierQuotations",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CountryId",
                table: "Suppliers",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CreatedById",
                table: "Suppliers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CurrencyId",
                table: "Suppliers",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_LastDeletedById",
                table: "Suppliers",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_LastUpdatedById",
                table: "Suppliers",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasures_CreatedById",
                table: "UnitOfMeasures",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasures_LastDeletedById",
                table: "UnitOfMeasures",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasures_LastUpdatedById",
                table: "UnitOfMeasures",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_userclaims_UserId",
                table: "userclaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_userlogins_UserId",
                table: "userlogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_userroles_RoleId",
                table: "userroles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_users_DepartmentId",
                table: "users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseArrivalLocations_CreatedById",
                table: "WarehouseArrivalLocations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseArrivalLocations_LastDeletedById",
                table: "WarehouseArrivalLocations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseArrivalLocations_LastUpdatedById",
                table: "WarehouseArrivalLocations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseArrivalLocations_WarehouseId",
                table: "WarehouseArrivalLocations",
                column: "WarehouseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationRacks_CreatedById",
                table: "WarehouseLocationRacks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationRacks_LastDeletedById",
                table: "WarehouseLocationRacks",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationRacks_LastUpdatedById",
                table: "WarehouseLocationRacks",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationRacks_WarehouseLocationId",
                table: "WarehouseLocationRacks",
                column: "WarehouseLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_CreatedById",
                table: "WarehouseLocations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_LastDeletedById",
                table: "WarehouseLocations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_LastUpdatedById",
                table: "WarehouseLocations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_WarehouseId",
                table: "WarehouseLocations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationShelves_CreatedById",
                table: "WarehouseLocationShelves",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationShelves_LastDeletedById",
                table: "WarehouseLocationShelves",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationShelves_LastUpdatedById",
                table: "WarehouseLocationShelves",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocationShelves_WarehouseLocationRackId",
                table: "WarehouseLocationShelves",
                column: "WarehouseLocationRackId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CreatedById",
                table: "Warehouses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_DepartmentId",
                table: "Warehouses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_LastDeletedById",
                table: "Warehouses",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_LastUpdatedById",
                table: "Warehouses",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_CreatedById",
                table: "WorkCenters",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_LastDeletedById",
                table: "WorkCenters",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_LastUpdatedById",
                table: "WorkCenters",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CreatedById",
                table: "WorkOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_LastDeletedById",
                table: "WorkOrders",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_LastUpdatedById",
                table: "WorkOrders",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_MasterProductionScheduleId",
                table: "WorkOrders",
                column: "MasterProductionScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProductionScheduleId",
                table: "WorkOrders",
                column: "ProductionScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalStages");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "BatchItem");

            migrationBuilder.DropTable(
                name: "BatchManufacturingRecords");

            migrationBuilder.DropTable(
                name: "BatchPackagingRecords");

            migrationBuilder.DropTable(
                name: "BillingSheets");

            migrationBuilder.DropTable(
                name: "BillOfMaterialItems");

            migrationBuilder.DropTable(
                name: "BinCardInformation");

            migrationBuilder.DropTable(
                name: "Charge");

            migrationBuilder.DropTable(
                name: "CompletedRequisitionItems");

            migrationBuilder.DropTable(
                name: "Configurations");

            migrationBuilder.DropTable(
                name: "FinishedProducts");

            migrationBuilder.DropTable(
                name: "FormAssignees");

            migrationBuilder.DropTable(
                name: "FormResponses");

            migrationBuilder.DropTable(
                name: "FormReviewers");

            migrationBuilder.DropTable(
                name: "ManufacturerMaterials");

            migrationBuilder.DropTable(
                name: "MassMaterialBatchMovements");

            migrationBuilder.DropTable(
                name: "MaterialBatchEvents");

            migrationBuilder.DropTable(
                name: "MaterialItemDistributions");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "PackageTypes");

            migrationBuilder.DropTable(
                name: "PasswordResets");

            migrationBuilder.DropTable(
                name: "ProductBillOfMaterials");

            migrationBuilder.DropTable(
                name: "ProductionActivityLogs");

            migrationBuilder.DropTable(
                name: "ProductionActivityStepResources");

            migrationBuilder.DropTable(
                name: "ProductionActivityStepUsers");

            migrationBuilder.DropTable(
                name: "ProductionActivityStepWorkCenters");

            migrationBuilder.DropTable(
                name: "ProductionScheduleItems");

            migrationBuilder.DropTable(
                name: "ProductionScheduleProducts");

            migrationBuilder.DropTable(
                name: "ProductionSteps");

            migrationBuilder.DropTable(
                name: "ProductPackages");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItem");

            migrationBuilder.DropTable(
                name: "QuestionOptions");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RequisitionApprovals");

            migrationBuilder.DropTable(
                name: "RevisedPurchaseOrderItem");

            migrationBuilder.DropTable(
                name: "roleclaims");

            migrationBuilder.DropTable(
                name: "RouteResources");

            migrationBuilder.DropTable(
                name: "RouteResponsibleRoles");

            migrationBuilder.DropTable(
                name: "RouteResponsibleUsers");

            migrationBuilder.DropTable(
                name: "RouteWorkCenters");

            migrationBuilder.DropTable(
                name: "ShelfMaterialBatches");

            migrationBuilder.DropTable(
                name: "ShipmentDiscrepancyItem");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "SourceRequisitionItems");

            migrationBuilder.DropTable(
                name: "Srs");

            migrationBuilder.DropTable(
                name: "StockTransferSources");

            migrationBuilder.DropTable(
                name: "SupplierManufacturers");

            migrationBuilder.DropTable(
                name: "SupplierQuotationItems");

            migrationBuilder.DropTable(
                name: "userclaims");

            migrationBuilder.DropTable(
                name: "userlogins");

            migrationBuilder.DropTable(
                name: "userroles");

            migrationBuilder.DropTable(
                name: "usertokens");

            migrationBuilder.DropTable(
                name: "Approvals");

            migrationBuilder.DropTable(
                name: "MaterialTypes");

            migrationBuilder.DropTable(
                name: "PurchaseOrderInvoices");

            migrationBuilder.DropTable(
                name: "CompletedRequisitions");

            migrationBuilder.DropTable(
                name: "FormFields");

            migrationBuilder.DropTable(
                name: "Responses");

            migrationBuilder.DropTable(
                name: "ShipmentInvoiceItems");

            migrationBuilder.DropTable(
                name: "BillOfMaterials");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "RevisedPurchaseOrders");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "WorkCenters");

            migrationBuilder.DropTable(
                name: "WarehouseLocationShelves");

            migrationBuilder.DropTable(
                name: "ShipmentDiscrepancies");

            migrationBuilder.DropTable(
                name: "ShipmentDiscrepancyTypes");

            migrationBuilder.DropTable(
                name: "MaterialBatches");

            migrationBuilder.DropTable(
                name: "StockTransfers");

            migrationBuilder.DropTable(
                name: "SupplierQuotations");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "FormSections");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "MasterProductionSchedules");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "WarehouseLocationRacks");

            migrationBuilder.DropTable(
                name: "ShipmentDocuments");

            migrationBuilder.DropTable(
                name: "Checklists");

            migrationBuilder.DropTable(
                name: "Grns");

            migrationBuilder.DropTable(
                name: "SourceRequisitions");

            migrationBuilder.DropTable(
                name: "WarehouseLocations");

            migrationBuilder.DropTable(
                name: "DistributedRequisitionMaterials");

            migrationBuilder.DropTable(
                name: "Manufacturers");

            migrationBuilder.DropTable(
                name: "RequisitionItems");

            migrationBuilder.DropTable(
                name: "ShipmentInvoices");

            migrationBuilder.DropTable(
                name: "WarehouseArrivalLocations");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Requisitions");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "MaterialCategories");

            migrationBuilder.DropTable(
                name: "ProductionActivitySteps");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Forms");

            migrationBuilder.DropTable(
                name: "Operations");

            migrationBuilder.DropTable(
                name: "ProductionActivities");

            migrationBuilder.DropTable(
                name: "ProductionSchedules");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropTable(
                name: "UnitOfMeasures");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
