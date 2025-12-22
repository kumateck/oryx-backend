using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedAtToJobRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobRequests_Equipments_EquipmentId",
                table: "JobRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "EquipmentId",
                table: "JobRequests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "DescriptionOfWork",
                table: "JobRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "JobRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedById",
                table: "JobRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToEmployeeId",
                table: "JobRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentInstrumentNumber",
                table: "JobRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HandlingType",
                table: "JobRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceId",
                table: "JobRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uuid", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedById = table.Column<Guid>(type: "uuid", nullable: true),
                    VerificationComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobExecutions_Employees_AssignedToEmployeeId",
                        column: x => x.AssignedToEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobExecutions_JobRequests_JobRequestId",
                        column: x => x.JobRequestId,
                        principalTable: "JobRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobExecutions_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobExecutions_users_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobExecutions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobExecutions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobExecutions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobExecutions_users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConsumedItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobOrderExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuotationItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityConsumed = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumedItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumedItems_JobExecutions_JobExecutionId",
                        column: x => x.JobExecutionId,
                        principalTable: "JobExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumedItems_UnitOfMeasures_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumedItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConsumedItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConsumedItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobOrderExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PerformedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobActivities_JobExecutions_JobExecutionId",
                        column: x => x.JobExecutionId,
                        principalTable: "JobExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobActivities_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobActivities_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobActivities_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobActivities_users_PerformedById",
                        column: x => x.PerformedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobOrderExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedById = table.Column<Guid>(type: "uuid", nullable: true),
                    VerificationComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequesterSatisfied = table.Column<bool>(type: "boolean", nullable: false),
                    RequesterComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOrderExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobOrderExecutions_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobOrderExecutions_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobOrderExecutions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobOrderExecutions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobOrderExecutions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobOrderExecutions_users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JobRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SelectedQuotationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceMemoId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobOrders_JobRequests_JobRequestId",
                        column: x => x.JobRequestId,
                        principalTable: "JobRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobOrders_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobOrders_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobOrders_users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobOrders_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobOrders_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobOrderServiceProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseReceived = table.Column<bool>(type: "boolean", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOrderServiceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobOrderServiceProviders_JobOrders_JobOrderId",
                        column: x => x.JobOrderId,
                        principalTable: "JobOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobOrderServiceProviders_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceQuotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServiceCharge = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedDays = table.Column<int>(type: "integer", nullable: false),
                    EstimatedCompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    NegotiatedServiceCharge = table.Column<decimal>(type: "numeric", nullable: true),
                    NegotiatedTotalCost = table.Column<decimal>(type: "numeric", nullable: true),
                    NegotiationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_ServiceQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceQuotations_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceQuotations_JobOrders_JobOrderId",
                        column: x => x.JobOrderId,
                        principalTable: "JobOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceQuotations_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceQuotations_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceQuotations_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceQuotations_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuotationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Supplier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NegotiatedUnitPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuotationItems_ServiceQuotations_ServiceQuotationId",
                        column: x => x.ServiceQuotationId,
                        principalTable: "ServiceQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuotationItems_UnitOfMeasures_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuotationItems_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuotationItems_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuotationItems_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceMemos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreedServiceCharge = table.Column<decimal>(type: "numeric", nullable: false),
                    AgreedMaterialsCost = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedCompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TermsAndConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SpecialInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceMemos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceMemos_JobOrders_JobOrderId",
                        column: x => x.JobOrderId,
                        principalTable: "JobOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceMemos_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceMemos_ServiceQuotations_ServiceQuotationId",
                        column: x => x.ServiceQuotationId,
                        principalTable: "ServiceQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceMemos_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceMemos_users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceMemos_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceMemos_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceMemoApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceMemoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    StageStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceMemoApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceMemoApprovals_Approvals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceMemoApprovals_ServiceMemos_ServiceMemoId",
                        column: x => x.ServiceMemoId,
                        principalTable: "ServiceMemos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceMemoApprovals_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceMemoApprovals_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceMemoApprovals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobRequests_AssignedById",
                table: "JobRequests",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequests_AssignedToEmployeeId",
                table: "JobRequests",
                column: "AssignedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequests_ServiceId",
                table: "JobRequests",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedItems_CreatedById",
                table: "ConsumedItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedItems_ItemId",
                table: "ConsumedItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedItems_JobExecutionId",
                table: "ConsumedItems",
                column: "JobExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedItems_JobOrderExecutionId",
                table: "ConsumedItems",
                column: "JobOrderExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedItems_LastDeletedById",
                table: "ConsumedItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedItems_LastUpdatedById",
                table: "ConsumedItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedItems_QuotationItemId",
                table: "ConsumedItems",
                column: "QuotationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedItems_UnitOfMeasureId",
                table: "ConsumedItems",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_JobActivities_CreatedById",
                table: "JobActivities",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobActivities_JobExecutionId",
                table: "JobActivities",
                column: "JobExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobActivities_JobOrderExecutionId",
                table: "JobActivities",
                column: "JobOrderExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobActivities_LastDeletedById",
                table: "JobActivities",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobActivities_LastUpdatedById",
                table: "JobActivities",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobActivities_PerformedById",
                table: "JobActivities",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_ApprovedById",
                table: "JobExecutions",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_AssignedById",
                table: "JobExecutions",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_AssignedToEmployeeId",
                table: "JobExecutions",
                column: "AssignedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_CreatedById",
                table: "JobExecutions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_JobRequestId",
                table: "JobExecutions",
                column: "JobRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_LastDeletedById",
                table: "JobExecutions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_LastUpdatedById",
                table: "JobExecutions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_VerifiedById",
                table: "JobExecutions",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderExecutions_ApprovedById",
                table: "JobOrderExecutions",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderExecutions_CreatedById",
                table: "JobOrderExecutions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderExecutions_JobOrderId",
                table: "JobOrderExecutions",
                column: "JobOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderExecutions_LastDeletedById",
                table: "JobOrderExecutions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderExecutions_LastUpdatedById",
                table: "JobOrderExecutions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderExecutions_ServiceProviderId",
                table: "JobOrderExecutions",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderExecutions_VerifiedById",
                table: "JobOrderExecutions",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_CreatedById",
                table: "JobOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_IssuedById",
                table: "JobOrders",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_JobRequestId",
                table: "JobOrders",
                column: "JobRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_LastDeletedById",
                table: "JobOrders",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_LastUpdatedById",
                table: "JobOrders",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_SelectedQuotationId",
                table: "JobOrders",
                column: "SelectedQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_ServiceId",
                table: "JobOrders",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderServiceProviders_JobOrderId",
                table: "JobOrderServiceProviders",
                column: "JobOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrderServiceProviders_ServiceProviderId",
                table: "JobOrderServiceProviders",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItems_CreatedById",
                table: "QuotationItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItems_ItemId",
                table: "QuotationItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItems_LastDeletedById",
                table: "QuotationItems",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItems_LastUpdatedById",
                table: "QuotationItems",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItems_ServiceQuotationId",
                table: "QuotationItems",
                column: "ServiceQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItems_UnitOfMeasureId",
                table: "QuotationItems",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemoApprovals_ApprovalId",
                table: "ServiceMemoApprovals",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemoApprovals_ApprovedById",
                table: "ServiceMemoApprovals",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemoApprovals_RoleId",
                table: "ServiceMemoApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemoApprovals_ServiceMemoId",
                table: "ServiceMemoApprovals",
                column: "ServiceMemoId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemoApprovals_UserId",
                table: "ServiceMemoApprovals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemos_CreatedById",
                table: "ServiceMemos",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemos_IssuedById",
                table: "ServiceMemos",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemos_JobOrderId",
                table: "ServiceMemos",
                column: "JobOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemos_LastDeletedById",
                table: "ServiceMemos",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemos_LastUpdatedById",
                table: "ServiceMemos",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemos_ServiceProviderId",
                table: "ServiceMemos",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMemos_ServiceQuotationId",
                table: "ServiceMemos",
                column: "ServiceQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceQuotations_CreatedById",
                table: "ServiceQuotations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceQuotations_CurrencyId",
                table: "ServiceQuotations",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceQuotations_JobOrderId",
                table: "ServiceQuotations",
                column: "JobOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceQuotations_LastDeletedById",
                table: "ServiceQuotations",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceQuotations_LastUpdatedById",
                table: "ServiceQuotations",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceQuotations_ServiceProviderId",
                table: "ServiceQuotations",
                column: "ServiceProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobRequests_Employees_AssignedToEmployeeId",
                table: "JobRequests",
                column: "AssignedToEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JobRequests_Equipments_EquipmentId",
                table: "JobRequests",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JobRequests_Services_ServiceId",
                table: "JobRequests",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JobRequests_users_AssignedById",
                table: "JobRequests",
                column: "AssignedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumedItems_JobOrderExecutions_JobOrderExecutionId",
                table: "ConsumedItems",
                column: "JobOrderExecutionId",
                principalTable: "JobOrderExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumedItems_QuotationItems_QuotationItemId",
                table: "ConsumedItems",
                column: "QuotationItemId",
                principalTable: "QuotationItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JobActivities_JobOrderExecutions_JobOrderExecutionId",
                table: "JobActivities",
                column: "JobOrderExecutionId",
                principalTable: "JobOrderExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobOrderExecutions_JobOrders_JobOrderId",
                table: "JobOrderExecutions",
                column: "JobOrderId",
                principalTable: "JobOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobOrders_ServiceQuotations_SelectedQuotationId",
                table: "JobOrders",
                column: "SelectedQuotationId",
                principalTable: "ServiceQuotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobRequests_Employees_AssignedToEmployeeId",
                table: "JobRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_JobRequests_Equipments_EquipmentId",
                table: "JobRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_JobRequests_Services_ServiceId",
                table: "JobRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_JobRequests_users_AssignedById",
                table: "JobRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceQuotations_JobOrders_JobOrderId",
                table: "ServiceQuotations");

            migrationBuilder.DropTable(
                name: "ConsumedItems");

            migrationBuilder.DropTable(
                name: "JobActivities");

            migrationBuilder.DropTable(
                name: "JobOrderServiceProviders");

            migrationBuilder.DropTable(
                name: "ServiceMemoApprovals");

            migrationBuilder.DropTable(
                name: "QuotationItems");

            migrationBuilder.DropTable(
                name: "JobExecutions");

            migrationBuilder.DropTable(
                name: "JobOrderExecutions");

            migrationBuilder.DropTable(
                name: "ServiceMemos");

            migrationBuilder.DropTable(
                name: "JobOrders");

            migrationBuilder.DropTable(
                name: "ServiceQuotations");

            migrationBuilder.DropIndex(
                name: "IX_JobRequests_AssignedById",
                table: "JobRequests");

            migrationBuilder.DropIndex(
                name: "IX_JobRequests_AssignedToEmployeeId",
                table: "JobRequests");

            migrationBuilder.DropIndex(
                name: "IX_JobRequests_ServiceId",
                table: "JobRequests");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "JobRequests");

            migrationBuilder.DropColumn(
                name: "AssignedById",
                table: "JobRequests");

            migrationBuilder.DropColumn(
                name: "AssignedToEmployeeId",
                table: "JobRequests");

            migrationBuilder.DropColumn(
                name: "EquipmentInstrumentNumber",
                table: "JobRequests");

            migrationBuilder.DropColumn(
                name: "HandlingType",
                table: "JobRequests");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "JobRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "EquipmentId",
                table: "JobRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DescriptionOfWork",
                table: "JobRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JobRequests_Equipments_EquipmentId",
                table: "JobRequests",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
