using System.Reflection;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.StpDocuments;
using DOMAIN.Entities.AttendanceRecords;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Charges;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Children;
using DOMAIN.Entities.CompanyWorkingDays;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.DamagedStocks;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.EmployeeHistories;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Holidays;
using DOMAIN.Entities.Instruments;
using DOMAIN.Entities.Invoices;
using DOMAIN.Entities.InventoryLedgers;
using DOMAIN.Entities.ItemGrns;
using DOMAIN.Entities.ItemInventoryTransactions;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.ItemShipments;
using DOMAIN.Entities.ItemStockRequisitions;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.LeaveEntitlements;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.LeaveTypes;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSampling;
using DOMAIN.Entities.MaterialSpecifications;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.Memos;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Organizations;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.Payroll;
using DOMAIN.Entities.Performance;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductAnalyticalRawData;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductSpecifications;
using DOMAIN.Entities.ProductsSampling;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.ProformaInvoices;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.RecoverableItemsReports;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Services;
using DOMAIN.Entities.ShiftAssignments;
using DOMAIN.Entities.ShiftSchedules;
using DOMAIN.Entities.ShiftTypes;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.StaffRequisitions;
using DOMAIN.Entities.StockAdjustments;
using DOMAIN.Entities.StockEntries;
using DOMAIN.Entities.Thresholds;
using DOMAIN.Entities.Tickets;
using DOMAIN.Entities.UniformityOfWeights;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.VendorQuotations;
using DOMAIN.Entities.Vendors;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.WorkOrders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Configuration = DOMAIN.Entities.Configurations.Configuration;
using ServiceProvider = DOMAIN.Entities.ServiceProviders.ServiceProvider;

namespace INFRASTRUCTURE.Context;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUserService currentUserService
) : IdentityDbContext<User, Role, Guid>(options)
{
    #region Auth
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    #endregion

    #region Organization

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Site> Sites { get; set; }

    #endregion

    #region Resources

    public DbSet<Resource> Resources { get; set; }

    #endregion

    #region Material

    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialType> MaterialTypes { get; set; }
    public DbSet<MaterialCategory> MaterialCategories { get; set; }

    public DbSet<Sr> Srs { get; set; }

    public DbSet<MaterialBatch> MaterialBatches { get; set; }
    public DbSet<MaterialBatchEvent> MaterialBatchEvents { get; set; }
    public DbSet<MassMaterialBatchMovement> MassMaterialBatchMovements { get; set; }
    public DbSet<DistributedRequisitionMaterial> DistributedRequisitionMaterials { get; set; }
    public DbSet<DistributedFinishedProduct> DistributedFinishedProducts { get; set; }
    public DbSet<MaterialItemDistribution> MaterialItemDistributions { get; set; }
    public DbSet<MaterialBatchReservedQuantity> MaterialBatchReservedQuantities { get; set; }
    public DbSet<MaterialReturnNote> MaterialReturnNotes { get; set; }
    public DbSet<MaterialReturnNoteFullReturn> MaterialReturnNoteFullReturns { get; set; }
    public DbSet<MaterialReturnNotePartialReturn> MaterialReturnNotePartialReturns { get; set; }
    public DbSet<MaterialDepartment> MaterialDepartments { get; set; }
    public DbSet<HoldingMaterialTransfer> HoldingMaterialTransfers { get; set; }
    public DbSet<HoldingMaterialTransferBatch> HoldingMaterialTransferBatches { get; set; }

    public DbSet<MaterialSpecification> MaterialSpecifications { get; set; }

    public DbSet<MaterialReject> MaterialRejects { get; set; }
    public DbSet<DistributeMaterial> DistributeMaterials { get; set; }

    #endregion

    #region UnitOfMeasure

    public DbSet<UnitOfMeasure> UnitOfMeasures { get; set; }

    #endregion

    #region PackageStyle

    public DbSet<PackageStyle> PackageStyles { get; set; }

    #endregion

    #region TermsOfPayment

    public DbSet<TermsOfPayment> TermsOfPayments { get; set; }

    #endregion

    #region DeliveryMode

    public DbSet<DeliveryMode> DeliveryModes { get; set; }

    #endregion

    #region Products

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<ProductBillOfMaterial> ProductBillOfMaterials { get; set; }
    public DbSet<FinishedProduct> FinishedProducts { get; set; }

    public DbSet<ProductPackage> ProductPackages { get; set; }
    public DbSet<ProductPackageSubstitute> ProductPackageSubstitutes { get; set; }
    public DbSet<PackageType> PackageTypes { get; set; }

    public DbSet<ProductSpecification> ProductSpecifications { get; set; }
    public DbSet<ProductPacking> ProductPackings { get; set; }

    #endregion

    #region BoM

    public DbSet<BillOfMaterial> BillOfMaterials { get; set; }
    public DbSet<BillOfMaterialItem> BillOfMaterialItems { get; set; }
    public DbSet<BillOfMaterialItemSubstitute> BillOfMaterialItemSubstitutes { get; set; }

    #endregion

    #region WorkOrder

    public DbSet<WorkOrder> WorkOrders { get; set; }
    public DbSet<ProductionStep> ProductionSteps { get; set; }

    #endregion

    #region ProductionSchedule

    public DbSet<ProductionSchedule> ProductionSchedules { get; set; }
    public DbSet<ProductionScheduleItem> ProductionScheduleItems { get; set; }
    public DbSet<ProductionScheduleProduct> ProductionScheduleProducts { get; set; }
    public DbSet<StockTransfer> StockTransfers { get; set; }
    public DbSet<StockTransferSource> StockTransferSources { get; set; }

    public DbSet<FinalPacking> FinalPackings { get; set; }
    public DbSet<FinalPackingMaterial> FinalPackingMaterials { get; set; }
    public DbSet<ProductionExtraPacking> ProductionExtraPackings { get; set; }
    public DbSet<ProductionExtraPackingApproval> ProductionExtraPackingApprovals { get; set; }

    public DbSet<MarketType> MarketTypes { get; set; }

    #endregion

    #region FinishedGoodsTransferNote

    public DbSet<FinishedGoodsTransferNote> FinishedGoodsTransferNotes { get; set; }
    public DbSet<FinishedGoodsTransferNoteApproval> FinishedGoodsTransferNoteApprovals { get; set; }
    public DbSet<FinishedProductBatchMovement> FinishedProductBatchMovements { get; set; }
    public DbSet<FinishedProductBatchEvent> FinishedProductBatchEvents { get; set; }

    #endregion

    #region WorkCenter

    public DbSet<WorkCenter> WorkCenters { get; set; }

    #endregion

    #region Operation

    public DbSet<Operation> Operations { get; set; }

    #endregion

    #region Route

    public DbSet<Route> Routes { get; set; }
    public DbSet<RouteResource> RouteResources { get; set; }
    public DbSet<RouteResponsibleUser> RouteResponsibleUsers { get; set; }
    public DbSet<RouteResponsibleRole> RouteResponsibleRoles { get; set; }
    public DbSet<RouteWorkCenter> RouteWorkCenters { get; set; }

    #endregion

    #region Configuration
    public DbSet<Configuration> Configurations { get; set; }
    #endregion

    #region Requisition

    public DbSet<Requisition> Requisitions { get; set; }
    public DbSet<RequisitionItem> RequisitionItems { get; set; }
    public DbSet<RequisitionApproval> RequisitionApprovals { get; set; }

    public DbSet<SourceRequisition> SourceRequisitions { get; set; }
    public DbSet<SourceRequisitionItem> SourceRequisitionItems { get; set; }
    public DbSet<SupplierQuotation> SupplierQuotations { get; set; }
    public DbSet<SupplierQuotationItem> SupplierQuotationItems { get; set; }

    #endregion

    #region Approvals

    public DbSet<Approval> Approvals { get; set; }
    public DbSet<ApprovalStage> ApprovalStages { get; set; }
    public DbSet<ApprovalActionLog> ApprovalActionLogs { get; set; }

    #endregion

    #region Warehouse

    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<WarehouseLocation> WarehouseLocations { get; set; }
    public DbSet<WarehouseLocationRack> WarehouseLocationRacks { get; set; }
    public DbSet<WarehouseLocationShelf> WarehouseLocationShelves { get; set; }
    public DbSet<ShelfMaterialBatch> ShelfMaterialBatches { get; set; }
    public DbSet<WarehouseArrivalLocation> WarehouseArrivalLocations { get; set; }
    public DbSet<SwapRequest> SwapRequests { get; set; }
    public DbSet<WarehouseLocationName> WarehouseLocationNames => Set<WarehouseLocationName>();

    #endregion

    #region BinCardInformation

    public DbSet<BinCardInformation> BinCardInformation { get; set; }
    public DbSet<ProductBinCardInformation> ProductBinCardInformation { get; set; }

    #endregion

    #region Procurement

    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierManufacturer> SupplierManufacturers { get; set; }
    public DbSet<SupplierCertification> SupplierCertifications { get; set; }
    public DbSet<SupplierContact> SupplierContacts { get; set; }
    public DbSet<SupplierBankDetail> SupplierBankDetails { get; set; }
    public DbSet<SupplierPricingAgreement> SupplierPricingAgreements { get; set; }
    public DbSet<SupplierPerformanceRecord> SupplierPerformanceRecords { get; set; }
    public DbSet<Manufacturer> Manufacturers { get; set; }
    public DbSet<ManufacturerMaterial> ManufacturerMaterials { get; set; }

    #endregion

    #region Country

    public DbSet<Country> Countries { get; set; }

    #endregion

    #region Department

    public DbSet<Department> Departments { get; set; }
    public DbSet<RoleDepartment> RoleDepartments { get; set; }

    #endregion

    #region Attachment

    public DbSet<Attachment> Attachments { get; set; }

    #endregion

    #region StpDocument

    public DbSet<StpDocument> StpDocuments { get; set; }

    public DbSet<StpDocumentVersion> StpDocumentVersions { get; set; }

    public DbSet<StpDocumentSignature> StpDocumentSignatures { get; set; }

    #endregion

    #region Currency

    public DbSet<Currency> Currencies { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }

    #endregion

    #region Payments

    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentApproval> PaymentApprovals { get; set; }

    #endregion

    #region Purchase Order

    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderApproval> PurchaseOrderApprovals { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
    public DbSet<PurchaseOrderInvoice> PurchaseOrderInvoices { get; set; }
    public DbSet<BillingSheet> BillingSheets { get; set; }
    public DbSet<BillingSheetCharge> BillingSheetCharges { get; set; }
    public DbSet<BillingSheetApproval> BillingSheetApprovals { get; set; }

    #endregion

    #region Shipment Document

    public DbSet<ShipmentDocument> ShipmentDocuments { get; set; }
    public DbSet<ShipmentDocumentApproval> ShipmentDocumentApprovals { get; set; }
    public DbSet<ShipmentInvoice> ShipmentInvoices { get; set; }
    public DbSet<ShipmentDiscrepancy> ShipmentDiscrepancies { get; set; }
    public DbSet<ShipmentDiscrepancyType> ShipmentDiscrepancyTypes { get; set; }
    public DbSet<ShipmentInvoiceItem> ShipmentInvoiceItems { get; set; }

    #endregion

    #region Form

    public DbSet<Form> Forms { get; set; }
    public DbSet<FormSection> FormSections { get; set; }
    public DbSet<FormField> FormFields { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<Response> Responses { get; set; }
    public DbSet<FormResponse> FormResponses { get; set; }
    public DbSet<FormReviewer> FormReviewers { get; set; }
    public DbSet<FormAssignee> FormAssignees => Set<FormAssignee>();
    public DbSet<FormFieldAssignee> FormFieldAssignees => Set<FormFieldAssignee>();
    public DbSet<ResponseApproval> ResponseApprovals { get; set; }

    public DbSet<FormulaDefinition> FormulaDefinitions { get; set; }
    public DbSet<QuestionFormulaDefinition> QuestionFormulaDefinitions { get; set; }
    public DbSet<FormulaRevision> FormulaRevisions { get; set; }
    public DbSet<FormulaRevisionAudit> FormulaRevisionAudits { get; set; }
    public DbSet<LegacyFormulaArtifact> LegacyFormulaArtifacts { get; set; }
    public DbSet<LegacyKeyMapping> LegacyKeyMappings { get; set; }
    public DbSet<FormRevision> FormRevisions { get; set; }
    public DbSet<FormRevisionAudit> FormRevisionAudits { get; set; }
    public DbSet<FormFieldRevision> FormFieldRevisions { get; set; }
    public DbSet<FormFieldFormulaConfiguration> FormFieldFormulaConfigurations { get; set; }
    public DbSet<ResponseFormulaSnapshot> ResponseFormulaSnapshots { get; set; }
    public DbSet<FormulaExecution> FormulaExecutions { get; set; }
    public DbSet<ResponseFormulaSubmissionSet> ResponseFormulaSubmissionSets { get; set; }
    public DbSet<ResponseFormulaSubmissionExecution> ResponseFormulaSubmissionExecutions { get; set; }
    public DbSet<FormulaMigrationRun> FormulaMigrationRuns { get; set; }
    public DbSet<FormulaMigrationItem> FormulaMigrationItems { get; set; }
    public DbSet<FormulaReconciliationResult> FormulaReconciliationResults { get; set; }

    #endregion

    #region Production

    public DbSet<BatchManufacturingRecord> BatchManufacturingRecords { get; set; }
    public DbSet<BatchPackagingRecord> BatchPackagingRecords { get; set; }
    public DbSet<ProductionActivity> ProductionActivities { get; set; }
    public DbSet<ProductionActivityStep> ProductionActivitySteps { get; set; }
    public DbSet<ProductionActivityStepUser> ProductionActivityStepUsers { get; set; }
    public DbSet<ProductionActivityStepResource> ProductionActivityStepResources { get; set; }
    public DbSet<ProductionActivityStepWorkCenter> ProductionActivityStepWorkCenters { get; set; }
    public DbSet<ProductionActivityLog> ProductionActivityLogs { get; set; }

    public DbSet<ProductionOrder> ProductionOrders { get; set; }
    public DbSet<ProductionOrderApprovals> ProductionOrderApprovals { get; set; }
    public DbSet<AllocateProductionOrder> AllocateProductionOrders { get; set; }
    public DbSet<AllocateProductionOrderApprovals> AllocateProductionOrderApprovals { get; set; }
    public DbSet<ProductionOrderWaybill> ProductionOrderWaybills => Set<ProductionOrderWaybill>();

    #endregion

    #region Checklist

    public DbSet<Checklist> Checklists { get; set; }

    #endregion

    #region GRN

    public DbSet<Grn> Grns { get; set; }

    #endregion

    #region Equipment

    public DbSet<Equipment> Equipments { get; set; }

    public DbSet<QcEquipment> QcEquipments { get; set; }
    public DbSet<QcEquipmentCategory> QcEquipmentCategories { get; set; }

    #endregion

    #region Charge

    public DbSet<Charge> Charges { get; set; }

    #endregion

    #region Employee

    public DbSet<Employee> Employees { get; set; }

    #endregion

    #region Children

    public DbSet<Child> Children { get; set; }

    #endregion

    #region Permission

    public DbSet<PermissionType> PermissionTypes { get; set; }

    #endregion

    #region Employement History

    public DbSet<EmploymentHistory> EmploymentHistories { get; set; }

    #endregion

    #region Designation

    public DbSet<Designation> Designations { get; set; }

    #endregion

    #region Leave
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveEntitlement> LeaveEntitlements { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<LeaveRequestApproval> LeaveRequestApprovals { get; set; }

    #endregion

    #region Overtime Request

    public DbSet<OvertimeRequest> OvertimeRequests { get; set; }
    public DbSet<OvertimeRequestApproval> OvertimeRequestApprovals { get; set; }

    #endregion

    #region Payroll

    public DbSet<EmployeeCompensation> EmployeeCompensations { get; set; }
    public DbSet<CompensationAllowance> CompensationAllowances { get; set; }
    public DbSet<PayGrade> PayGrades { get; set; }
    public DbSet<PayeTaxBand> PayeTaxBands { get; set; }
    public DbSet<SsnitRate> SsnitRates { get; set; }
    public DbSet<PayrollDeduction> PayrollDeductions { get; set; }
    public DbSet<PayrollAddition> PayrollAdditions { get; set; }
    public DbSet<EmployeeTaxRelief> EmployeeTaxReliefs { get; set; }
    public DbSet<PayrollRun> PayrollRuns { get; set; }
    public DbSet<PayrollRunApproval> PayrollRunApprovals { get; set; }
    public DbSet<Payslip> Payslips { get; set; }
    public DbSet<PayslipLineItem> PayslipLineItems { get; set; }

    #endregion

    #region Performance

    public DbSet<PerformanceCycle> PerformanceCycles { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<PerformanceReview> PerformanceReviews { get; set; }
    public DbSet<GoalRating> GoalRatings { get; set; }
    public DbSet<PerformanceReviewApproval> PerformanceReviewApprovals { get; set; }

    #endregion

    #region Shifts

    public DbSet<ShiftType> ShiftTypes { get; set; }

    #endregion

    #region Shift Schedule
    public DbSet<ShiftSchedule> ShiftSchedules { get; set; }

    public DbSet<ShiftAssignment> ShiftAssignments { get; set; }

    #endregion

    #region Shift Category

    public DbSet<ShiftCategory> ShiftCategories { get; set; }
    public DbSet<WorkingHoursPolicy> WorkingHoursPolicies { get; set; }

    #endregion

    #region Company Working Days

    public DbSet<CompanyWorkingDays> CompanyWorkingDays { get; set; }

    #endregion

    #region Holidays

    public DbSet<Holiday> Holidays { get; set; }

    #endregion

    #region Standard Test Procedures

    public DbSet<MaterialStandardTestProcedure> MaterialStandardTestProcedures { get; set; }

    public DbSet<ProductStandardTestProcedure> ProductStandardTestProcedures { get; set; }

    #endregion

    #region Analytical Raw Data

    public DbSet<MaterialAnalyticalRawData> MaterialAnalyticalRawData { get; set; }

    public DbSet<ProductAnalyticalRawData> ProductAnalyticalRawData { get; set; }

    #endregion

    #region Staff Requisitions

    public DbSet<StaffRequisition> StaffRequisitions { get; set; }

    public DbSet<StaffRequisitionApproval> StaffRequisitionApprovals { get; set; }

    #endregion

    #region Attendance Records
    public DbSet<AttendanceRecords> AttendanceRecords { get; set; }

    #endregion

    #region Alerts

    public DbSet<Alert> Alerts { get; set; }
    public DbSet<AlertRole> AlertRoles { get; set; }
    public DbSet<AlertUser> AlertUsers { get; set; }

    #endregion

    #region AnalyticalTestRequests

    public DbSet<AnalyticalTestRequest> AnalyticalTestRequests { get; set; }
    public DbSet<AnalyticalTestRequestAssignee> AnalyticalTestRequestAssignees { get; set; }
    public DbSet<ProductState> ProductStates { get; set; }
    public DbSet<DOMAIN.Entities.OosInvestigations.OosInvestigation> OosInvestigations { get; set; }

    #endregion

    #region Quality Audits

    public DbSet<DOMAIN.Entities.QualityAudits.QualityAudit> QualityAudits { get; set; }
    public DbSet<DOMAIN.Entities.QualityAudits.QualityAuditTeamMember> QualityAuditTeamMembers { get; set; }
    public DbSet<DOMAIN.Entities.QualityAudits.AuditChecklistTemplate> AuditChecklistTemplates { get; set; }
    public DbSet<DOMAIN.Entities.QualityAudits.AuditChecklistTemplateItem> AuditChecklistTemplateItems { get; set; }
    public DbSet<DOMAIN.Entities.QualityAudits.AuditChecklistResponse> AuditChecklistResponses { get; set; }
    public DbSet<DOMAIN.Entities.QualityAudits.AuditFinding> AuditFindings { get; set; }
    public DbSet<DOMAIN.Entities.QualityAudits.AuditCorrectiveAction> AuditCorrectiveActions { get; set; }

    #endregion

    #region Research & Development

    public DbSet<DOMAIN.Entities.RndProjects.RndProject> RndProjects { get; set; }
    public DbSet<DOMAIN.Entities.RndProjects.RndProjectApprovals> RndProjectApprovals { get; set; }
    public DbSet<DOMAIN.Entities.RndFormulations.RndFormulation> RndFormulations { get; set; }
    public DbSet<DOMAIN.Entities.RndFormulations.RndFormulationItem> RndFormulationItems { get; set; }
    public DbSet<DOMAIN.Entities.RndFormulations.RndFormulationItemSubstitute> RndFormulationItemSubstitutes { get; set; }
    public DbSet<DOMAIN.Entities.RndTrialBatches.RndTrialBatch> RndTrialBatches { get; set; }

    #endregion

    #region Sample Products

    public DbSet<ProductSampling> ProductSamplings { get; set; }

    #endregion

    #region Sample Materials

    public DbSet<MaterialSampling> MaterialSamplings { get; set; }
    public DbSet<PreSampleChecklist> PreSampleChecklists { get; set; }
    #endregion

    #region Notification

    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationRead> NotificationReads { get; set; }

    #endregion

    #region Customers

    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerContact> CustomerContacts { get; set; }
    public DbSet<CustomerPricingAgreement> CustomerPricingAgreements { get; set; }
    public DbSet<CustomerQuotation> CustomerQuotations { get; set; }
    public DbSet<CustomerQuotationItem> CustomerQuotationItems { get; set; }
    public DbSet<CustomerQuotationApproval> CustomerQuotationApprovals { get; set; }

    #endregion

    #region Instrument

    public DbSet<Instrument> Instruments { get; set; }

    #endregion

    #region Uniformity Of Weight

    public DbSet<UniformityOfWeight> UniformityOfWeights { get; set; }
    public DbSet<UniformityOfWeightResponse> UniformityOfWeightResponses { get; set; }

    #endregion

    #region Services

    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceProvider> ServiceProviders { get; set; }

    #endregion

    #region Items

    public DbSet<Item> Items { get; set; }
    public DbSet<ItemGrn> ItemGrns { get; set; }
    public DbSet<ItemCategory> ItemCategories { get; set; }
    public DbSet<ItemShipmentInvoice> ItemShipmentInvoices { get; set; }
    public DbSet<ItemShipmentInvoiceItem> ItemShipmentInvoiceItems { get; set; }
    public DbSet<ItemShipmentDocument> ItemShipmentDocuments { get; set; }
    public DbSet<ItemBillingSheet> ItemBillingSheets { get; set; }
    public DbSet<ItemBillingSheetCharge> ItemBillingSheetCharges { get; set; }

    #endregion

    #region Vendors
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<VendorItem> VendorItems { get; set; }

    #endregion

    #region Proforma Invoice

    public DbSet<ProformaInvoice> ProformaInvoices { get; set; }

    // public DbSet<InventoryProformaInvoice> InventoryProformaInvoices { get; set; }
    public DbSet<ProformaInvoiceProduct> ProformaInvoiceProducts { get; set; }
    public DbSet<ProformaInvoiceApproval> ProformaInvoiceApprovals { get; set; }

    #endregion

    #region Invoice

    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceAmount> InvoiceAmounts { get; set; }

    #endregion

    #region Item Stock Requisitions

    public DbSet<ItemInventoryTransaction> ItemInventoryTransactions { get; set; }
    public DbSet<ItemStockRequisition> ItemStockRequisitions { get; set; }
    public DbSet<ItemStockRequisitionItem> ItemStockRequisitionItems { get; set; }
    public DbSet<IssueItemStockRequisition> IssueItemStockRequisitions { get; set; }

    #endregion

    #region Inventory Procurement

    public DbSet<InventoryPurchaseRequisition> InventoryPurchaseRequisitions { get; set; }
    public DbSet<InventoryPurchaseRequisitionItem> InventoryPurchaseRequisitionItems { get; set; }

    public DbSet<SourceInventoryRequisition> SourceInventoryRequisitions { get; set; }

    public DbSet<MarketRequisition> MarketRequisitions { get; set; }

    public DbSet<VendorQuotation> VendorQuotations { get; set; }

    public DbSet<VendorQuotationItem> VendorQuotationItems { get; set; }

    public DbSet<MarketRequisitionVendor> MarketRequisitionVendors { get; set; }

    public DbSet<Memo> Memos { get; set; }

    public DbSet<MemoItem> MemoItems { get; set; }

    #endregion

    #region Damaged Stocks
    public DbSet<DamagedStock> DamagedStocks { get; set; }

    #endregion

    #region Recoverable Items Report

    public DbSet<RecoverableItemReport> RecoverableItemReports { get; set; }

    #endregion

    #region Stock Entries

    public DbSet<StockEntry> StockEntries { get; set; }
    public DbSet<StockAdjustment> StockAdjustments { get; set; }
    public DbSet<StockAdjustmentLine> StockAdjustmentLines { get; set; }
    public DbSet<StockAdjustmentApproval> StockAdjustmentApprovals { get; set; }
    public DbSet<InventoryLedger> InventoryLedgers { get; set; }
    #endregion

    #region Job Requests

    public DbSet<JobRequest> JobRequests { get; set; }
    public DbSet<JobRequestApproval> JobRequestApprovals { get; set; }
    public DbSet<JobExecution> JobExecutions { get; set; }
    public DbSet<JobActivity> JobActivities { get; set; }
    public DbSet<ConsumedItem> ConsumedItems { get; set; }
    public DbSet<JobOrder> JobOrders { get; set; }
    public DbSet<JobOrderServiceProvider> JobOrderServiceProviders { get; set; }
    public DbSet<JobOrderExecution> JobOrderExecutions { get; set; }
    public DbSet<ServiceQuotation> ServiceQuotations { get; set; }
    public DbSet<QuotationItem> QuotationItems { get; set; }
    public DbSet<ServiceProformaInvoice> ServiceProformaInvoices { get; set; }
    public DbSet<ServiceProformaInvoiceItem> ServiceProformaInvoiceItems { get; set; }
    public DbSet<ServiceMemo> ServiceMemos { get; set; }
    public DbSet<ServiceMemoApproval> ServiceMemoApprovals { get; set; }

    #endregion

    #region IT Support Tickets

    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketActivity> TicketActivities { get; set; }

    #endregion

    #region Item Transaction Logs

    public DbSet<ItemTransactionLog> ItemTransactionLogs { get; set; }

    #endregion

    #region Reagent

    public DbSet<Reagent> Reagents => Set<Reagent>();

    #endregion

    #region Threshold

    public DbSet<Threshold> Threshold => Set<Threshold>();

    #endregion

    // #region TenantFilter
    // private void ApplyTenantQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IBaseEntity, IOrganizationType
    // {
    //     modelBuilder.Entity<TEntity>().HasQueryFilter(entity => entity.OrganizationName == tenantProvider.Tenant && !entity.DeletedAt.HasValue);
    // }
    // #endregion

    #region SoftDeleteFilter
    private static void ApplyDeletedAtFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IBaseEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
    }
    #endregion


    /// <summary>
    /// When true, <see cref="SaveChanges()"/> refuses to persist a price without the
    /// unit it was quoted in. Set once at startup from the
    /// <c>Procurement:EnforcePriceUoM</c> setting so it can be switched off without a
    /// redeploy if a legacy path trips it in production.
    /// </summary>
    public static bool EnforcePriceUoM { get; set; }

    public override int SaveChanges()
    {
        FormulaChangeGuard.Validate(ChangeTracker);
        SaveEntity();
        ValidatePriceUoM();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        FormulaChangeGuard.Validate(ChangeTracker);
        SaveEntity();
        ValidatePriceUoM();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// A stored price is uninterpretable without its PriceUoM: a quantity in grams
    /// against a price per kilogram is wrong by a factor of 1000. Enforcing it here,
    /// over a change tracker that is already being walked, makes the bad state
    /// unreachable from any repository, mapper, or future feature rather than relying
    /// on each write path to remember.
    /// </summary>
    private void ValidatePriceUoM()
    {
        if (!EnforcePriceUoM)
            return;

        foreach (
            var entry in ChangeTracker
                .Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
        )
        {
            string entityName;
            decimal price;
            string priceUoM;

            switch (entry.Entity)
            {
                case PurchaseOrderItem item:
                    (entityName, price, priceUoM) = (
                        nameof(PurchaseOrderItem),
                        item.Price,
                        item.PriceUoM
                    );
                    break;
                case ShipmentInvoiceItem item:
                    (entityName, price, priceUoM) = (
                        nameof(ShipmentInvoiceItem),
                        item.Price,
                        item.PriceUoM
                    );
                    break;
                case SupplierQuotationItem item:
                    (entityName, price, priceUoM) = (
                        nameof(SupplierQuotationItem),
                        item.QuotedPrice ?? 0m,
                        item.PriceUoM
                    );
                    break;
                default:
                    continue;
            }

            if (price > 0 && string.IsNullOrWhiteSpace(priceUoM))
            {
                throw new InvalidOperationException(
                    $"{entityName} was saved with a price of {price} but no PriceUoM. "
                        + "A price must always carry the unit it was quoted in."
                );
            }
        }
    }

    private void SaveEntity()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e =>
                e
                    is {
                        Entity: BaseEntity,
                        State: EntityState.Added or EntityState.Modified or EntityState.Deleted
                    }
            );

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.CreatedById = currentUserService.UserId;
                    break;

                case EntityState.Modified:
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.LastUpdatedById = currentUserService.UserId;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entity.DeletedAt = DateTime.UtcNow;
                    entity.LastDeletedById = currentUserService.UserId;
                    break;
            }
        }
    }

    public bool ShouldNotFilterProducts =>
        currentUserService.DepartmentType == nameof(DepartmentType.NonProduction)
        || currentUserService.DepartmentType == nameof(DepartmentType.RnD);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Call base method
        base.OnModelCreating(modelBuilder);

        ConfigureTableMappings(modelBuilder);
        ConfigureAutoIncludes(modelBuilder);
        ConfigureQueryFilters(modelBuilder);
        ConfigureRelationships(modelBuilder);
        ConfigureConstraints(modelBuilder);
    }

    private void ConfigureTableMappings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("userroles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("userclaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("userlogins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("roleclaims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("usertokens");
    }

    private void ConfigureAutoIncludes(ModelBuilder modelBuilder)
    {
        #region User Entities
        modelBuilder.Entity<User>().Navigation(p => p.Department).AutoInclude();
        #endregion

        #region Department Entities
        modelBuilder.Entity<Department>().Navigation(p => p.Warehouses).AutoInclude();
        // modelBuilder.Entity<DepartmentWarehouse>().Navigation(p => p.Warehouse).AutoInclude();
        #endregion

        #region Warehouse Entities
        modelBuilder.Entity<Warehouse>().Navigation(p => p.Locations).AutoInclude();
        modelBuilder.Entity<WarehouseLocation>().Navigation(p => p.Racks).AutoInclude();
        modelBuilder.Entity<WarehouseLocationRack>().Navigation(p => p.Shelves).AutoInclude();
        modelBuilder
            .Entity<MaterialItemDistribution>()
            .Navigation(p => p.ShipmentInvoiceItem)
            .AutoInclude();

        #endregion

        #region Material Entities
        modelBuilder.Entity<Material>().Navigation(p => p.Batches).AutoInclude();
        modelBuilder.Entity<MaterialBatch>().Navigation(p => p.Events).AutoInclude();
        modelBuilder.Entity<MaterialBatch>().Navigation(p => p.ReservedQuantities).AutoInclude();
        modelBuilder.Entity<ManufacturerMaterial>().Navigation(p => p.Material).AutoInclude();
        #endregion

        #region Product Entities
        modelBuilder.Entity<Product>().Navigation(p => p.Category).AutoInclude();
        modelBuilder.Entity<Product>().Navigation(p => p.BaseUoM).AutoInclude();
        modelBuilder.Entity<Product>().Navigation(p => p.Equipment).AutoInclude();
        modelBuilder.Entity<Product>().Navigation(p => p.Department).AutoInclude();
        modelBuilder.Entity<Product>().Navigation(p => p.Prices).AutoInclude();
        modelBuilder.Entity<FinishedProduct>().Navigation(fp => fp.UoM).AutoInclude();
        modelBuilder.Entity<ProductPackage>().Navigation(pp => pp.Material).AutoInclude();
        modelBuilder.Entity<ProductPackage>().Navigation(pp => pp.Substitutes).AutoInclude();
        modelBuilder.Entity<ProductPackageSubstitute>().Navigation(pps => pps.SubstituteMaterial).AutoInclude();
        modelBuilder
            .Entity<ProductBillOfMaterial>()
            .Navigation(pbm => pbm.BillOfMaterial)
            .AutoInclude();
        #endregion

        #region Bill of Material Entities
        modelBuilder.Entity<BillOfMaterial>().Navigation(bom => bom.Items).AutoInclude();
        modelBuilder.Entity<BillOfMaterial>().Navigation(bom => bom.Product).AutoInclude();
        modelBuilder.Entity<BillOfMaterialItem>().Navigation(bomi => bomi.Material).AutoInclude();
        modelBuilder.Entity<BillOfMaterialItem>().Navigation(bomi => bomi.Substitutes).AutoInclude();
        modelBuilder.Entity<BillOfMaterialItemSubstitute>().Navigation(bomis => bomis.SubstituteMaterial).AutoInclude();
        modelBuilder
            .Entity<BillOfMaterialItem>()
            .Navigation(bomi => bomi.MaterialType)
            .AutoInclude();
        modelBuilder.Entity<BillOfMaterialItem>().Navigation(bomi => bomi.BaseUoM).AutoInclude();
        #endregion

        #region Route Entities
        modelBuilder.Entity<Route>().Navigation(r => r.Operation).AutoInclude();
        modelBuilder.Entity<RouteResponsibleUser>().Navigation(r => r.User).AutoInclude();
        modelBuilder.Entity<RouteResponsibleRole>().Navigation(r => r.Role).AutoInclude();
        modelBuilder.Entity<RouteWorkCenter>().Navigation(r => r.WorkCenter).AutoInclude();
        modelBuilder.Entity<RouteResource>().Navigation(rr => rr.Resource).AutoInclude();
        #endregion

        #region Approval Entities
        modelBuilder.Entity<Approval>().Navigation(r => r.ApprovalStages).AutoInclude();
        modelBuilder.Entity<ApprovalStage>().Navigation(r => r.User).AutoInclude();
        modelBuilder.Entity<ApprovalStage>().Navigation(r => r.Role).AutoInclude();
        #endregion

        #region Requsition Entities
        modelBuilder.Entity<Requisition>().Navigation(r => r.CreatedBy).AutoInclude();
        modelBuilder.Entity<SourceRequisition>().Navigation(r => r.CreatedBy).AutoInclude();
        modelBuilder.Entity<RequisitionItem>().Navigation(r => r.UoM).AutoInclude();
        modelBuilder.Entity<SourceRequisitionItem>().Navigation(r => r.UoM).AutoInclude();
        #endregion

        #region Purchase Order Entites

        modelBuilder.Entity<PurchaseOrder>().Navigation(p => p.Supplier).AutoInclude();
        modelBuilder.Entity<PurchaseOrder>().Navigation(p => p.Items).AutoInclude();
        modelBuilder.Entity<PurchaseOrder>().Navigation(p => p.RevisedPurchaseOrders).AutoInclude();
        modelBuilder.Entity<PurchaseOrderItem>().Navigation(p => p.Material).AutoInclude();
        modelBuilder.Entity<PurchaseOrderItem>().Navigation(p => p.UoM).AutoInclude();
        modelBuilder.Entity<PurchaseOrderItem>().Navigation(p => p.Currency).AutoInclude();

        modelBuilder.Entity<RevisedPurchaseOrder>().Navigation(p => p.Material).AutoInclude();
        modelBuilder.Entity<RevisedPurchaseOrder>().Navigation(p => p.UoM).AutoInclude();
        modelBuilder.Entity<RevisedPurchaseOrder>().Navigation(p => p.Currency).AutoInclude();
        modelBuilder.Entity<RevisedPurchaseOrder>().Navigation(p => p.MaterialBefore).AutoInclude();
        modelBuilder.Entity<RevisedPurchaseOrder>().Navigation(p => p.UomBefore).AutoInclude();
        modelBuilder.Entity<RevisedPurchaseOrder>().Navigation(p => p.CurrencyBefore).AutoInclude();

        modelBuilder.Entity<RevisedPurchaseOrderItem>().Navigation(p => p.Material).AutoInclude();
        modelBuilder.Entity<RevisedPurchaseOrderItem>().Navigation(p => p.UoM).AutoInclude();
        modelBuilder.Entity<RevisedPurchaseOrderItem>().Navigation(p => p.Currency).AutoInclude();

        modelBuilder.Entity<PurchaseOrderInvoice>().Navigation(p => p.BatchItems).AutoInclude();
        modelBuilder.Entity<PurchaseOrderInvoice>().Navigation(p => p.Charges).AutoInclude();
        modelBuilder.Entity<BatchItem>().Navigation(b => b.Manufacturer).AutoInclude();
        modelBuilder.Entity<PurchaseOrderCharge>().Navigation(b => b.Currency).AutoInclude();

        modelBuilder.Entity<BillingSheet>().Navigation(b => b.Supplier).AutoInclude();
        modelBuilder.Entity<ShipmentInvoice>().Navigation(b => b.Supplier).AutoInclude();

        #endregion

        #region Material Entities

        modelBuilder.Entity<MaterialBatch>().Navigation(p => p.UoM).AutoInclude();
        modelBuilder.Entity<MaterialBatch>().Navigation(p => p.ShelfMaterialBatches).AutoInclude();
        modelBuilder
            .Entity<DistributedRequisitionMaterial>()
            .Navigation(p => p.DistributedRequisitionItems)
            .AutoInclude();
        modelBuilder.Entity<DistributedRequisitionItem>().Navigation(p => p.UoM).AutoInclude();
        modelBuilder
            .Entity<DistributedRequisitionItem>()
            .Navigation(p => p.RequisitionItem)
            .AutoInclude();
        modelBuilder
            .Entity<DistributedRequisitionItem>()
            .Navigation(p => p.Warehouse)
            .AutoInclude();

        #endregion

        #region Supplier Entities

        modelBuilder.Entity<Supplier>().Navigation(p => p.Country).AutoInclude();
        modelBuilder.Entity<Supplier>().Navigation(p => p.Currency).AutoInclude();
        modelBuilder.Entity<SupplierManufacturer>().Navigation(p => p.UoM).AutoInclude();

        #endregion

        #region Form Entities

        //modelBuilder.Entity<Form>().Navigation(p => p.Sections).AutoInclude();
        //modelBuilder.Entity<FormSection>().Navigation(p => p.Fields).AutoInclude();
        //modelBuilder.Entity<FormField>().Navigation(p => p.Question).AutoInclude();

        modelBuilder.Entity<Question>().Navigation(p => p.Options).AutoInclude();

        #endregion

        #region Production Schedule Entities

        modelBuilder
            .Entity<ProductionScheduleProduct>()
            .Navigation(p => p.MarketType)
            .AutoInclude();

        #endregion

        #region Production Activity

        modelBuilder
            .Entity<ProductionActivityStepResource>()
            .Navigation(p => p.Resource)
            .AutoInclude();
        modelBuilder.Entity<ProductionActivityStepUser>().Navigation(p => p.User).AutoInclude();
        modelBuilder
            .Entity<ProductionActivityStepWorkCenter>()
            .Navigation(p => p.WorkCenter)
            .AutoInclude();
        modelBuilder.Entity<ProductionActivityLog>().Navigation(p => p.User).AutoInclude();

        #endregion

        #region Equipment

        modelBuilder.Entity<Equipment>().Navigation(p => p.Department).AutoInclude();
        modelBuilder.Entity<Equipment>().Navigation(p => p.UoM).AutoInclude();

        #endregion

        #region ShipmentInvoice

        modelBuilder.Entity<ShipmentInvoiceItem>().Navigation(p => p.Manufacturer).AutoInclude();
        modelBuilder.Entity<ShipmentInvoiceItem>().Navigation(p => p.Material).AutoInclude();
        modelBuilder.Entity<ShipmentInvoiceItem>().Navigation(p => p.UoM).AutoInclude();

        #endregion

        #region Designation

        modelBuilder.Entity<Designation>().Navigation(p => p.Departments).AutoInclude();

        #endregion

        #region Item Grns

        modelBuilder.Entity<ItemGrn>().Navigation(p => p.Item).AutoInclude();
        modelBuilder.Entity<ItemGrn>().Navigation(p => p.Supplier).AutoInclude();

        #endregion
    }

    private void ConfigureQueryFilters(ModelBuilder modelBuilder)
    {
        #region Auth Filters

        modelBuilder.Entity<User>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder
            .Entity<PasswordReset>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.User.DeletedAt.HasValue
            );

        #endregion

        #region Alert Filters

        modelBuilder.Entity<Alert>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Product Filters

        modelBuilder
            .Entity<Product>()
            .HasQueryFilter(entity =>
                ShouldNotFilterProducts
                || (
                    !entity.DeletedAt.HasValue
                    && entity.DepartmentId == currentUserService.DepartmentId
                )
            );
        modelBuilder
            .Entity<ProductPackage>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue
                && entity.Product != null
                && !entity.Product.DeletedAt.HasValue
            );
        modelBuilder.Entity<ProductCategory>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder
            .Entity<FinishedProduct>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue
                && entity.Product != null
                && !entity.Product.DeletedAt.HasValue
            );
        modelBuilder
            .Entity<ProductSpecification>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder
            .Entity<ProductPacking>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue
                && entity.Product != null
                && !entity.Product.DeletedAt.HasValue
            );

        #endregion

        #region Material Filters

        modelBuilder.Entity<Material>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder
            .Entity<MaterialBatch>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.Material.DeletedAt.HasValue
            ); // && entity.Status == BatchStatus.Available);
        modelBuilder
            .Entity<Sr>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.MaterialBatch.DeletedAt.HasValue
            ); // && entity.Status == BatchStatus.Available);
        modelBuilder
            .Entity<ShelfMaterialBatch>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.MaterialBatch.DeletedAt.HasValue
            ); // && entity.Status == BatchStatus.Available);
        modelBuilder
            .Entity<MaterialBatchEvent>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue
                && !entity.Batch.DeletedAt.HasValue
                && !entity.User.DeletedAt.HasValue
            ); // && !entity.Batch.IsFrozen);
        modelBuilder
            .Entity<MassMaterialBatchMovement>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.Batch.DeletedAt.HasValue
            ); //  && !entity.Batch.IsFrozen);
        modelBuilder
            .Entity<MaterialCategory>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<MaterialType>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder
            .Entity<MaterialBatchReservedQuantity>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.MaterialBatch.DeletedAt.HasValue
            );
        // modelBuilder.Entity<FinishedProductBatchMovement>().HasQueryFilter(entity => !entity.Batch.DeletedAt.HasValue);
        // modelBuilder.Entity<FinishedProductBatchEvent>().HasQueryFilter(entity => !entity.Batch.DeletedAt.HasValue);
        // modelBuilder.Entity<MaterialReturnNote>().HasQueryFilter(entity => !entity.Product.DeletedAt.HasValue);
        // modelBuilder.Entity<MaterialReturnNoteFullReturn>()
        //     .HasQueryFilter(entity => !entity.DestinationWarehouse.DeletedAt.HasValue);
        modelBuilder
            .Entity<MaterialReturnNotePartialReturn>()
            .HasQueryFilter(entity => !entity.DestinationWarehouse.DeletedAt.HasValue);
        modelBuilder
            .Entity<ProductionExtraPacking>()
            .HasQueryFilter(entity => !entity.Material.DeletedAt.HasValue);
        modelBuilder
            .Entity<MaterialSpecification>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Requisition Filters

        modelBuilder
            .Entity<RequisitionApproval>()
            .HasQueryFilter(entity => entity.Requisition != null);

        #endregion

        #region WorkOrder Filters

        modelBuilder.Entity<WorkOrder>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder
            .Entity<ProductionStep>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.WorkOrder.DeletedAt.HasValue
            );

        #endregion

        #region BoM Filters

        modelBuilder
            .Entity<BillOfMaterial>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue
                && entity.Product != null
                && !entity.Product.DeletedAt.HasValue
            );
        modelBuilder
            .Entity<ProductBillOfMaterial>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.BillOfMaterial.DeletedAt.HasValue
            );
        modelBuilder
            .Entity<ProductBillOfMaterial>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue
                && entity.Product != null
                && !entity.Product.DeletedAt.HasValue
            );
        modelBuilder.Entity<BillOfMaterialItem>().HasQueryFilter(entity => entity.Material != null);

        #endregion

        #region Route Filters

        modelBuilder.Entity<Route>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder
            .Entity<RouteResponsibleUser>()
            .HasQueryFilter(entity =>
                !entity.Route.DeletedAt.HasValue && !entity.User.DeletedAt.HasValue
            );
        modelBuilder
            .Entity<RouteResponsibleRole>()
            .HasQueryFilter(entity =>
                !entity.Route.DeletedAt.HasValue && !entity.Role.DeletedAt.HasValue
            );
        modelBuilder
            .Entity<RouteWorkCenter>()
            .HasQueryFilter(entity =>
                !entity.Route.DeletedAt.HasValue && !entity.WorkCenter.DeletedAt.HasValue
            );
        modelBuilder
            .Entity<RouteResource>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.Resource.DeletedAt.HasValue
            );

        #endregion

        #region Configuration Filters

        modelBuilder.Entity<Configuration>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Miscellaneous Filters

        modelBuilder.Entity<Resource>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<UnitOfMeasure>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<Operation>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region MasterProductionSchedule Filters

        modelBuilder
            .Entity<MasterProductionSchedule>()
            .HasQueryFilter(mps => mps.Product != null && !mps.Product.DeletedAt.HasValue);
        modelBuilder
            .Entity<ProductionScheduleProduct>()
            .HasQueryFilter(mps => mps.Product != null && !mps.Product.DeletedAt.HasValue);
        modelBuilder
            .Entity<ProductionSchedule>()
            .HasQueryFilter(mps => !mps.DeletedAt.HasValue && mps.Products.Count != 0);
        modelBuilder
            .Entity<ProductionScheduleItem>()
            .HasQueryFilter(mps => !mps.ProductionSchedule.DeletedAt.HasValue);
        // modelBuilder.Entity<FinalPacking>()
        //     .HasQueryFilter(mps => mps.Product != null && !mps.ProductionSchedule.DeletedAt.HasValue);
        modelBuilder
            .Entity<FinalPackingMaterial>()
            .HasQueryFilter(mps => mps.FinalPacking != null && !mps.Material.DeletedAt.HasValue);

        #endregion

        #region Requisition Filters

        modelBuilder.Entity<Requisition>().HasQueryFilter(r => !r.DeletedAt.HasValue);
        modelBuilder.Entity<SourceRequisition>().HasQueryFilter(r => !r.DeletedAt.HasValue);
        modelBuilder.Entity<RequisitionItem>().HasQueryFilter(r => !r.Material.DeletedAt.HasValue);
        modelBuilder
            .Entity<SourceRequisitionItem>()
            .HasQueryFilter(r => !r.SourceRequisition.DeletedAt.HasValue);

        #endregion

        #region Approval Filters

        modelBuilder.Entity<Approval>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<ApprovalStage>().HasQueryFilter(a => !a.Approval.DeletedAt.HasValue);
        modelBuilder
            .Entity<LeaveRequestApproval>()
            .HasQueryFilter(a => !a.Approval.DeletedAt.HasValue);
        modelBuilder
            .Entity<OvertimeRequestApproval>()
            .HasQueryFilter(a => !a.Approval.DeletedAt.HasValue);

        #endregion

        #region Procurement Filters

        modelBuilder.Entity<Supplier>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder
            .Entity<SupplierManufacturer>()
            .HasQueryFilter(a => !a.Supplier.DeletedAt.HasValue);
        modelBuilder.Entity<SupplierCertification>()
            .HasQueryFilter(a => !a.DeletedAt.HasValue && !a.Supplier.DeletedAt.HasValue);
        modelBuilder.Entity<SupplierContact>()
            .HasQueryFilter(a => !a.DeletedAt.HasValue && !a.Supplier.DeletedAt.HasValue);
        modelBuilder.Entity<SupplierBankDetail>()
            .HasQueryFilter(a => !a.DeletedAt.HasValue && !a.Supplier.DeletedAt.HasValue);
        modelBuilder.Entity<SupplierPricingAgreement>()
            .HasQueryFilter(a => !a.DeletedAt.HasValue && !a.Supplier.DeletedAt.HasValue);
        modelBuilder.Entity<SupplierPerformanceRecord>()
            .HasQueryFilter(a => !a.DeletedAt.HasValue && !a.Supplier.DeletedAt.HasValue);
        modelBuilder.Entity<Manufacturer>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<ManufacturerMaterial>().HasQueryFilter(a => !a.DeletedAt.HasValue);

        #endregion

        #region Department Filters

        modelBuilder.Entity<Department>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder
            .Entity<MaterialDepartment>()
            .HasQueryFilter(a => !a.Department.DeletedAt.HasValue);

        #endregion

        #region Warehouse Filters

        modelBuilder
            .Entity<Warehouse>()
            .HasQueryFilter(a =>
                ShouldNotFilterProducts
                || (a.DepartmentId == currentUserService.DepartmentId && !a.DeletedAt.HasValue)
            );

        modelBuilder
            .Entity<WarehouseLocation>()
            .HasQueryFilter(a =>
                ShouldNotFilterProducts
                || (
                    a.Warehouse != null
                    && a.Warehouse.DepartmentId == currentUserService.DepartmentId
                    && !a.DeletedAt.HasValue
                )
            );

        modelBuilder
            .Entity<WarehouseLocationRack>()
            .HasQueryFilter(a =>
                ShouldNotFilterProducts
                || (
                    a.WarehouseLocation != null
                    && a.WarehouseLocation.Warehouse != null
                    && a.WarehouseLocation.Warehouse.DepartmentId == currentUserService.DepartmentId
                    && !a.DeletedAt.HasValue
                )
            );

        modelBuilder
            .Entity<WarehouseLocationShelf>()
            .HasQueryFilter(a =>
                ShouldNotFilterProducts
                || (
                    a.WarehouseLocationRack != null
                    && a.WarehouseLocationRack.WarehouseLocation != null
                    && a.WarehouseLocationRack.WarehouseLocation.Warehouse != null
                    && a.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId
                        == currentUserService.DepartmentId
                    && !a.DeletedAt.HasValue
                )
            );

        modelBuilder
            .Entity<WarehouseArrivalLocation>()
            .HasQueryFilter(a =>
                ShouldNotFilterProducts
                || (
                    a.Warehouse != null
                    && a.Warehouse.DepartmentId == currentUserService.DepartmentId
                    && !a.DeletedAt.HasValue
                )
            );

        modelBuilder
            .Entity<MaterialItemDistribution>()
            .HasQueryFilter(a => a.ShipmentInvoiceItem != null);

        #endregion

        #region DistributedRequisitionMaterial Filters

        modelBuilder
            .Entity<DistributedRequisitionMaterial>()
            .HasQueryFilter(a =>
                ShouldNotFilterProducts
                || (
                    a.WarehouseArrivalLocation.Warehouse.DepartmentId
                        == currentUserService.DepartmentId
                    && !a.DeletedAt.HasValue
                )
            );

        modelBuilder
            .Entity<Checklist>()
            .HasQueryFilter(a => a.DistributedRequisitionMaterial != null);

        #endregion

        #region Attachment Filter

        modelBuilder.Entity<Attachment>().HasQueryFilter(a => !a.DeletedAt.HasValue);

        #endregion

        #region StpDocument

        modelBuilder.Entity<StpDocument>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<StpDocumentVersion>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<StpDocumentSignature>().HasQueryFilter(a => !a.DeletedAt.HasValue);

        modelBuilder
            .Entity<StpDocument>()
            .HasMany(d => d.Versions)
            .WithOne(v => v.StpDocument)
            .HasForeignKey(v => v.StpDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<StpDocument>()
            .HasOne(d => d.CurrentDraftVersion)
            .WithMany()
            .HasForeignKey(d => d.CurrentDraftVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<StpDocument>()
            .HasOne(d => d.EffectiveVersion)
            .WithMany()
            .HasForeignKey(d => d.EffectiveVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<StpDocumentVersion>()
            .HasMany(v => v.Signatures)
            .WithOne(s => s.StpDocumentVersion)
            .HasForeignKey(s => s.StpDocumentVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StpDocument>().HasIndex(d => new { d.OwnerType, d.OwnerId }).IsUnique();
        modelBuilder.Entity<StpDocument>().Property(d => d.OwnerType).IsRequired().HasMaxLength(64);
        modelBuilder
            .Entity<StpDocumentVersion>()
            .HasIndex(version => new { version.StpDocumentId, version.VersionNumber })
            .IsUnique();
        modelBuilder.Entity<StpDocumentVersion>().Property(v => v.StorageKey).IsRequired().HasMaxLength(512);
        modelBuilder.Entity<StpDocumentVersion>().Property(v => v.FileName).IsRequired().HasMaxLength(255);
        modelBuilder.Entity<StpDocumentVersion>().Property(v => v.Sha256).IsRequired().HasMaxLength(64);
        modelBuilder.Entity<StpDocumentSignature>().Property(s => s.Meaning).IsRequired().HasMaxLength(500);
        modelBuilder.Entity<StpDocument>().ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_StpDocuments_OwnerType",
                "\"OwnerType\" IN ('MaterialStandardTestProcedure', 'ProductStandardTestProcedure')"
            );
            table.HasCheckConstraint("CK_StpDocuments_Status", "\"Status\" IN (0, 1, 2, 3)");
        });
        modelBuilder.Entity<StpDocumentVersion>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_StpDocumentVersions_Source", "\"Source\" IN (0, 1, 2, 3)");
            table.HasCheckConstraint("CK_StpDocumentVersions_VersionNumber", "\"VersionNumber\" > 0");
            table.HasCheckConstraint("CK_StpDocumentVersions_Size", "\"Size\" > 0");
        });
        modelBuilder.Entity<StpDocumentSignature>().ToTable(table =>
            table.HasCheckConstraint("CK_StpDocumentSignatures_Action", "\"Action\" IN (0, 1, 2)"));

        #endregion

        #region Currency

        modelBuilder.Entity<Currency>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<ExchangeRate>().HasQueryFilter(a => !a.DeletedAt.HasValue);

        #endregion

        #region Payments

        modelBuilder.Entity<Payment>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder
            .Entity<PaymentApproval>()
            .HasQueryFilter(a => !a.Payment.DeletedAt.HasValue);

        #endregion

        #region Purchase Order

        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder
            .Entity<PurchaseOrderApproval>()
            .HasQueryFilter(a => !a.PurchaseOrder.DeletedAt.HasValue);
        modelBuilder
            .Entity<PurchaseOrderItem>()
            .HasQueryFilter(a => !a.PurchaseOrder.DeletedAt.HasValue);
        modelBuilder.Entity<PurchaseOrderInvoice>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder
            .Entity<PurchaseOrderInvoice>()
            .HasQueryFilter(a => !a.PurchaseOrder.DeletedAt.HasValue);
        modelBuilder
            .Entity<BatchItem>()
            .HasQueryFilter(a => !a.PurchaseOrderInvoice.DeletedAt.HasValue);
        modelBuilder
            .Entity<PurchaseOrderCharge>()
            .HasQueryFilter(a => !a.PurchaseOrderInvoice.DeletedAt.HasValue);
        modelBuilder.Entity<BillingSheet>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder
            .Entity<BillingSheetApproval>()
            .HasQueryFilter(a => !a.BillingSheet.DeletedAt.HasValue);
        modelBuilder
            .Entity<RevisedPurchaseOrderItem>()
            .HasQueryFilter(a => !a.Material.DeletedAt.HasValue);

        #endregion

        #region SupplierQuotation

        modelBuilder
            .Entity<SupplierQuotation>()
            .HasQueryFilter(a => !a.Supplier.DeletedAt.HasValue);
        modelBuilder
            .Entity<SupplierQuotationItem>()
            .HasQueryFilter(a => !a.Material.DeletedAt.HasValue);

        #endregion

        #region Shipment Document

        modelBuilder.Entity<ShipmentDocument>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder
            .Entity<ShipmentDiscrepancy>()
            .HasQueryFilter(a => !a.DeletedAt.HasValue && !a.ShipmentDocument.DeletedAt.HasValue);
        modelBuilder.Entity<ShipmentInvoice>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder
            .Entity<ShipmentDiscrepancyItem>()
            .HasQueryFilter(a => !a.ShipmentDiscrepancy.DeletedAt.HasValue);
        modelBuilder
            .Entity<ShipmentInvoiceItem>()
            .HasQueryFilter(a => !a.ShipmentInvoice.DeletedAt.HasValue);

        #endregion

        #region Form Filter

        modelBuilder.Entity<Form>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<FormSection>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder
            .Entity<FormField>()
            .HasQueryFilter(a => !a.FormSection.DeletedAt.HasValue && !a.DeletedAt.HasValue);
        modelBuilder
            .Entity<FormReviewer>()
            .HasQueryFilter(a => !a.User.DeletedAt.HasValue && !a.Form.DeletedAt.HasValue);
        modelBuilder
            .Entity<FormResponse>()
            .HasQueryFilter(a => a.FormField != null && !a.FormField.DeletedAt.HasValue);
        modelBuilder.Entity<Response>().HasQueryFilter(a => !a.Form.DeletedAt.HasValue);
        modelBuilder.Entity<Response>()
            .HasIndex(a => a.BatchManufacturingRecordId);
        modelBuilder.Entity<Response>()
            .HasIndex(a => new { a.BatchManufacturingRecordId, a.ProductionActivityStepId })
            .IsUnique()
            .HasFilter(
                "\"BatchManufacturingRecordId\" IS NOT NULL AND \"ProductionActivityStepId\" IS NOT NULL"
            );

        modelBuilder.Entity<Question>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<QuestionOption>().HasQueryFilter(a => !a.DeletedAt.HasValue);

        #endregion

        #region Production

        modelBuilder.Entity<BatchManufacturingRecord>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<BatchPackagingRecord>().HasQueryFilter(a => !a.DeletedAt.HasValue);

        // modelBuilder.Entity<ProductionActivity>()
        //     .HasQueryFilter(a => !a.ProductionScheduleProduct.Cancelled == false);
        modelBuilder
            .Entity<ProductionActivityStep>()
            .HasQueryFilter(a => !a.Operation.DeletedAt.HasValue);
        modelBuilder
            .Entity<ProductionActivityStepResource>()
            .HasQueryFilter(a => !a.Resource.DeletedAt.HasValue);
        modelBuilder
            .Entity<ProductionActivityStepWorkCenter>()
            .HasQueryFilter(a => !a.WorkCenter.DeletedAt.HasValue);
        modelBuilder
            .Entity<ProductionActivityStepUser>()
            .HasQueryFilter(a => !a.User.DeletedAt.HasValue);
        modelBuilder
            .Entity<ProductionActivityLog>()
            .HasQueryFilter(a => a.ProductionActivity != null);

        #endregion

        #region Stock Transfer

        modelBuilder
            .Entity<StockTransfer>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.Material.DeletedAt.HasValue
            ); // && entity.Status == BatchStatus.Available);
        modelBuilder
            .Entity<StockTransferSource>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue
                && !entity.FromDepartment.DeletedAt.HasValue
                && !entity.ToDepartment.DeletedAt.HasValue
            ); // && entity.Status == BatchStatus.Available);
        #endregion

        #region Equipment

        modelBuilder
            .Entity<Equipment>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.Department.DeletedAt.HasValue
            );

        modelBuilder.Entity<QcEquipment>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<QcEquipment>().HasOne(item => item.CalibrationCertificateAttachment)
            .WithMany().HasForeignKey(item => item.CalibrationCertificateAttachmentId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region Finished Goods

        modelBuilder
            .Entity<FinishedGoodsTransferNote>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Employee

        modelBuilder.Entity<Employee>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Holiday

        modelBuilder.Entity<Holiday>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Shift Type

        modelBuilder.Entity<ShiftType>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Shift Schedule

        modelBuilder
            .Entity<ShiftSchedule>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue && !entity.Department.DeletedAt.HasValue
            );

        #endregion

        #region Shift Assignments

        modelBuilder
            .Entity<ShiftAssignment>()
            .HasQueryFilter(entity =>
                !entity.ShiftCategory.DeletedAt.HasValue
                && !entity.Employee.DeletedAt.HasValue
                && !entity.ShiftSchedules.DeletedAt.HasValue
                && !entity.ShiftType.DeletedAt.HasValue
            );

        #endregion

        #region Leave Requests

        modelBuilder
            .Entity<LeaveRequest>()
            .HasQueryFilter(entity =>
                !entity.DeletedAt.HasValue
                && !entity.Employee.DeletedAt.HasValue
                && !entity.LeaveType.DeletedAt.HasValue
            );

        #endregion

        #region Leave Type

        modelBuilder.Entity<LeaveType>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Designation

        modelBuilder.Entity<Designation>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Department

        modelBuilder.Entity<Department>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Analytical Test Requests

        modelBuilder
            .Entity<AnalyticalTestRequest>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Material Analytical Raw Data

        modelBuilder
            .Entity<MaterialAnalyticalRawData>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Product Analytical Raw Data

        modelBuilder
            .Entity<ProductAnalyticalRawData>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Material Standard Test Procedure

        modelBuilder
            .Entity<MaterialStandardTestProcedure>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Product Standard Test Procedure

        modelBuilder
            .Entity<ProductStandardTestProcedure>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Material Sampling

        modelBuilder
            .Entity<MaterialSampling>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Product Sampling

        modelBuilder.Entity<ProductSampling>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region GRN

        modelBuilder
            .Entity<Grn>()
            .HasQueryFilter(entity =>
                ShouldNotFilterProducts
                || (
                    !entity.DeletedAt.HasValue
                    && entity.CreatedBy.DepartmentId == currentUserService.DepartmentId
                )
            );
        #endregion

        #region Overtime Request

        modelBuilder.Entity<OvertimeRequest>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Staff Requisitions

        modelBuilder
            .Entity<StaffRequisition>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Customers

        modelBuilder.Entity<Customer>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<CustomerContact>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.Customer.DeletedAt.HasValue);
        modelBuilder.Entity<CustomerPricingAgreement>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.Customer.DeletedAt.HasValue);
        modelBuilder.Entity<CustomerQuotation>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.Customer.DeletedAt.HasValue);
        modelBuilder.Entity<CustomerQuotationItem>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.CustomerQuotation.DeletedAt.HasValue);
        modelBuilder.Entity<Invoice>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder
            .Entity<InvoiceAmount>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Invoice.DeletedAt.HasValue);

        #endregion

        #region Production Orders

        modelBuilder.Entity<ProductionOrder>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Instruments

        modelBuilder.Entity<Instrument>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<Instrument>().HasOne(item => item.CalibrationCertificateAttachment)
            .WithMany().HasForeignKey(item => item.CalibrationCertificateAttachmentId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region Work Center

        modelBuilder.Entity<WorkCenter>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Services

        modelBuilder.Entity<Service>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<ServiceProvider>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Items

        modelBuilder.Entity<Item>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<ItemGrn>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region  Vendors

        modelBuilder.Entity<Vendor>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Item Stock Requisitions

        modelBuilder
            .Entity<ItemStockRequisition>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Inventory Procurement

        modelBuilder
            .Entity<InventoryPurchaseRequisition>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Damaged Stocks

        modelBuilder.Entity<DamagedStock>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Job Requests

        modelBuilder.Entity<JobRequest>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region IT Support Tickets

        modelBuilder.Entity<Ticket>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Attendance Record Filter

        modelBuilder
            .Entity<AttendanceRecords>()
            .HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion

        #region Role Filter

        modelBuilder.Entity<Role>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);

        #endregion
    }

    private void ConfigureConstraints(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DOMAIN.Entities.QualityAudits.QualityAudit>().HasOne(item => item.ProductionOrder)
            .WithMany().HasForeignKey(item => item.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.QualityAudits.QualityAudit>().HasOne(item => item.Material)
            .WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.QualityAudits.QualityAudit>().HasOne(item => item.Product)
            .WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.QualityAudits.QualityAudit>().HasOne(item => item.Supplier)
            .WithMany().HasForeignKey(item => item.SupplierId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.QualityAudits.QualityAudit>().HasOne(item => item.LeadAuditor)
            .WithMany().HasForeignKey(item => item.LeadAuditorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.QualityAudits.QualityAuditTeamMember>().HasOne(item => item.User)
            .WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.QualityAudits.AuditCorrectiveAction>().HasOne(item => item.ResponsiblePerson)
            .WithMany().HasForeignKey(item => item.ResponsiblePersonId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DOMAIN.Entities.RndProjects.RndProject>().HasOne(item => item.Department)
            .WithMany().HasForeignKey(item => item.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.RndProjects.RndProject>().HasOne(item => item.RequestedBy)
            .WithMany().HasForeignKey(item => item.RequestedById).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.RndProjects.RndProject>().HasOne(item => item.Product)
            .WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.RndProjects.RndProject>().HasOne(item => item.QtppForm)
            .WithMany().HasForeignKey(item => item.QtppFormId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.RndProjects.RndProject>().HasOne(item => item.Attachment)
            .WithMany().HasForeignKey(item => item.AttachmentId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DOMAIN.Entities.RndFormulations.RndFormulationItem>().HasOne(item => item.Material)
            .WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.RndFormulations.RndFormulationItem>().HasOne(item => item.MaterialType)
            .WithMany().HasForeignKey(item => item.MaterialTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.RndFormulations.RndFormulationItem>().HasOne(item => item.BaseUoM)
            .WithMany().HasForeignKey(item => item.BaseUoMId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.RndFormulations.RndFormulationItemSubstitute>().HasOne(item => item.SubstituteMaterial)
            .WithMany().HasForeignKey(item => item.SubstituteMaterialId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DOMAIN.Entities.RndTrialBatches.RndTrialBatch>().HasOne(item => item.RndFormulation)
            .WithMany().HasForeignKey(item => item.RndFormulationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.RndTrialBatches.RndTrialBatch>().HasOne(item => item.BatchSizeUoM)
            .WithMany().HasForeignKey(item => item.BatchSizeUoMId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.RndTrialBatches.RndTrialBatch>().HasOne(item => item.PerformedBy)
            .WithMany().HasForeignKey(item => item.PerformedById).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DOMAIN.Entities.RndTrialBatches.RndTrialBatch>().HasOne(item => item.ProtocolForm)
            .WithMany().HasForeignKey(item => item.ProtocolFormId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Requisition>().HasOne(item => item.RndTrialBatch)
            .WithMany().HasForeignKey(item => item.RndTrialBatchId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Customer>().HasOne(item => item.TermsOfPayment)
            .WithMany().HasForeignKey(item => item.TermsOfPaymentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Customer>().HasOne(item => item.Currency)
            .WithMany().HasForeignKey(item => item.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerContact>().HasOne(item => item.Customer)
            .WithMany(item => item.Contacts).HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerPricingAgreement>().HasOne(item => item.Customer)
            .WithMany(item => item.PricingAgreements).HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerPricingAgreement>().HasOne(item => item.Product)
            .WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerPricingAgreement>().HasOne(item => item.ProductPacking)
            .WithMany().HasForeignKey(item => item.ProductPackingId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerPricingAgreement>().HasOne(item => item.Currency)
            .WithMany().HasForeignKey(item => item.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerQuotation>().HasOne(item => item.Customer)
            .WithMany().HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerQuotation>().HasOne(item => item.Currency)
            .WithMany().HasForeignKey(item => item.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerQuotationItem>().HasOne(item => item.ProductPacking)
            .WithMany().HasForeignKey(item => item.ProductPackingId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionOrder>().HasOne(item => item.SourceCustomerQuotation)
            .WithOne().HasForeignKey<ProductionOrder>(item => item.SourceCustomerQuotationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerContact>().HasIndex(item => item.CustomerId)
            .IsUnique().HasFilter("\"DeletedAt\" IS NULL AND \"IsPrimary\" = TRUE");
        modelBuilder.Entity<CustomerPricingAgreement>()
            .HasIndex(item => new { item.CustomerId, item.ProductId, item.ProductPackingId, item.EffectiveFrom });
        modelBuilder.Entity<CustomerQuotation>().HasIndex(item => item.Code)
            .IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        modelBuilder.Entity<ProductionOrder>().HasIndex(item => item.SourceCustomerQuotationId)
            .IsUnique().HasFilter("\"SourceCustomerQuotationId\" IS NOT NULL");
        modelBuilder.Entity<CustomerQuotationApproval>().HasIndex(item => new
            { item.ApprovalId, item.CustomerQuotationId, item.Order, item.UserId, item.RoleId }).IsUnique();

        // Requisition Approvals
        modelBuilder
            .Entity<RequisitionApproval>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.RequisitionId,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        // Billing Sheet Approvals
        modelBuilder
            .Entity<BillingSheetApproval>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.BillingSheetId,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        // Payment approvals and rate history are idempotent by workflow/rate date.
        modelBuilder
            .Entity<PaymentApproval>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.PaymentId,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        modelBuilder
            .Entity<ExchangeRate>()
            .HasIndex(rate => new { rate.CurrencyId, rate.EffectiveDate })
            .IsUnique();

        modelBuilder
            .Entity<Payment>()
            .HasIndex(payment => new
            {
                payment.PayableType,
                payment.PayableId,
                payment.Reference,
            })
            .IsUnique();

        modelBuilder
            .Entity<InvoiceAmount>()
            .HasIndex(amount => new { amount.InvoiceId, amount.CurrencyId })
            .IsUnique();

        modelBuilder.Entity<SupplierCertification>()
            .HasIndex(item => new { item.SupplierId, item.CertificateNumber }).IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        modelBuilder.Entity<SupplierBankDetail>()
            .HasIndex(item => new { item.SupplierId, item.AccountNumber, item.CurrencyId }).IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        modelBuilder.Entity<SupplierContact>().HasIndex(item => item.SupplierId).IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL AND \"IsPrimary\" = TRUE");
        modelBuilder.Entity<SupplierPricingAgreement>()
            .HasIndex(item => new { item.SupplierId, item.MaterialId, item.UoMId, item.EffectiveFrom });
        modelBuilder.Entity<SupplierPerformanceRecord>()
            .HasIndex(item => new { item.SupplierId, item.PeriodStart, item.PeriodEnd }).IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        // Purchase Order Approvals
        modelBuilder
            .Entity<PurchaseOrderApproval>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.PurchaseOrderId,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        // Leave Request Approvals
        modelBuilder
            .Entity<LeaveRequestApproval>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.LeaveRequestId,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        // Overtime Request Approvals
        modelBuilder
            .Entity<OvertimeRequestApproval>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.OvertimeRequestId,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        // Response Approvals
        modelBuilder
            .Entity<ResponseApproval>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.ResponseId,
                a.ApprovalRound,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        // Production Order Approvals
        modelBuilder
            .Entity<ProductionOrderApprovals>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.ProductionOrderId,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        // RndProject Approvals
        modelBuilder
            .Entity<DOMAIN.Entities.RndProjects.RndProjectApprovals>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.RndProjectId,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        // Shipment Document Approvals
        modelBuilder
            .Entity<ShipmentDocumentApproval>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.ShipmentDocumentId,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        // Proforma Invoice Approvals
        modelBuilder
            .Entity<ProformaInvoiceApproval>()
            .HasIndex(a => new
            {
                a.ApprovalId,
                a.ProformaInvoiceId,
                a.Order,
                a.UserId,
                a.RoleId,
            })
            .IsUnique();

        modelBuilder
            .Entity<ShelfMaterialBatch>()
            .HasIndex(x => new { x.WarehouseLocationShelfId, x.MaterialBatchId })
            .HasDatabaseName("IX_ShelfMaterialBatch_Unique_Shelf_Batch")
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
    }

    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        #region Cashflow

        modelBuilder.Entity<ExchangeRate>()
            .HasOne(rate => rate.Currency)
            .WithMany()
            .HasForeignKey(rate => rate.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InvoiceAmount>()
            .HasOne(amount => amount.Currency)
            .WithMany()
            .HasForeignKey(amount => amount.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(payment => payment.Currency)
            .WithMany()
            .HasForeignKey(payment => payment.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(payment => payment.RecordedBy)
            .WithMany()
            .HasForeignKey(payment => payment.RecordedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(payment => payment.BillingSheetCharge)
            .WithMany(charge => charge.Payments)
            .HasForeignKey(payment => payment.BillingSheetChargeId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region Supplier relationship management

        modelBuilder.Entity<SupplierCertification>().HasOne(item => item.Supplier)
            .WithMany(item => item.Certifications).HasForeignKey(item => item.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierCertification>().HasOne(item => item.Attachment)
            .WithMany().HasForeignKey(item => item.AttachmentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierContact>().HasOne(item => item.Supplier)
            .WithMany(item => item.Contacts).HasForeignKey(item => item.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierBankDetail>().HasOne(item => item.Supplier)
            .WithMany(item => item.BankDetails).HasForeignKey(item => item.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierBankDetail>().HasOne(item => item.Currency)
            .WithMany().HasForeignKey(item => item.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierPricingAgreement>().HasOne(item => item.Supplier)
            .WithMany(item => item.PricingAgreements).HasForeignKey(item => item.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierPricingAgreement>().HasOne(item => item.Currency)
            .WithMany().HasForeignKey(item => item.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierPricingAgreement>().HasOne(item => item.Material)
            .WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierPricingAgreement>().HasOne(item => item.UoM)
            .WithMany().HasForeignKey(item => item.UoMId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierPerformanceRecord>().HasOne(item => item.Supplier)
            .WithMany(item => item.PerformanceRecords).HasForeignKey(item => item.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region Employee

        modelBuilder.Entity<Employee>().OwnsOne(f => f.Mother);
        modelBuilder.Entity<Employee>().OwnsOne(f => f.Father);
        modelBuilder.Entity<Employee>().OwnsOne(f => f.Spouse);
        modelBuilder.Entity<Employee>().OwnsOne(f => f.EmergencyContact);
        modelBuilder.Entity<Employee>().OwnsOne(f => f.NextOfKin);

        modelBuilder
            .Entity<Employee>()
            .OwnsMany(
                e => e.Children,
                b =>
                {
                    b.WithOwner().HasForeignKey("EmployeeId");
                    b.Property<Guid>("Id");
                    b.HasKey("Id");
                }
            );

        modelBuilder
            .Entity<Employee>()
            .OwnsMany(
                e => e.EducationBackground,
                b =>
                {
                    b.WithOwner().HasForeignKey("EmployeeId");
                    b.Property<Guid>("Id");
                    b.HasKey("Id");
                }
            );

        modelBuilder
            .Entity<Employee>()
            .OwnsMany(
                e => e.EmploymentHistory,
                b =>
                {
                    b.WithOwner().HasForeignKey("EmployeeId");
                    b.Property<Guid>("Id");
                    b.HasKey("Id");
                }
            );

        #endregion

        #region Job Management

        // JobOrder has many ServiceQuotations
        modelBuilder
            .Entity<JobOrder>()
            .HasMany(jo => jo.Quotations)
            .WithOne(sq => sq.JobOrder)
            .HasForeignKey(sq => sq.JobOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // JobOrder has one selected ServiceQuotation (without inverse navigation)
        modelBuilder
            .Entity<JobOrder>()
            .HasOne(jo => jo.SelectedQuotation)
            .WithMany()
            .HasForeignKey(jo => jo.SelectedQuotationId)
            .OnDelete(DeleteBehavior.Restrict);

        // JobOrder has one ServiceProformaInvoice (without inverse navigation on ServiceProformaInvoice side)
        modelBuilder
            .Entity<JobOrder>()
            .HasOne(jo => jo.ServiceProformaInvoice)
            .WithOne(spi => spi.JobOrder)
            .HasForeignKey<ServiceProformaInvoice>(spi => spi.JobOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // JobOrder has one ServiceMemo (without inverse navigation on ServiceMemo side)
        modelBuilder
            .Entity<JobOrder>()
            .HasOne(jo => jo.ServiceMemo)
            .WithOne(sm => sm.JobOrder)
            .HasForeignKey<ServiceMemo>(sm => sm.JobOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // JobOrder has one JobOrderExecution
        modelBuilder
            .Entity<JobOrder>()
            .HasOne(jo => jo.Execution)
            .WithOne(e => e.JobOrder)
            .HasForeignKey<JobOrderExecution>(e => e.JobOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // JobActivity can belong to either JobExecution or JobOrderExecution
        modelBuilder
            .Entity<JobActivity>()
            .HasOne(ja => ja.JobExecution)
            .WithMany(je => je.Activities)
            .HasForeignKey(ja => ja.JobExecutionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        modelBuilder
            .Entity<JobActivity>()
            .HasOne(ja => ja.JobOrderExecution)
            .WithMany(joe => joe.Activities)
            .HasForeignKey(ja => ja.JobOrderExecutionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // ConsumedItem can belong to either JobExecution or JobOrderExecution
        modelBuilder
            .Entity<ConsumedItem>()
            .HasOne(ci => ci.JobExecution)
            .WithMany(je => je.ConsumedItems)
            .HasForeignKey(ci => ci.JobExecutionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        modelBuilder
            .Entity<ConsumedItem>()
            .HasOne(ci => ci.JobOrderExecution)
            .WithMany(joe => joe.ConsumedItems)
            .HasForeignKey(ci => ci.JobOrderExecutionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // ServiceProformaInvoice has many ServiceProformaInvoiceItems
        modelBuilder
            .Entity<ServiceProformaInvoice>()
            .HasMany(spi => spi.Items)
            .WithOne(spii => spii.ServiceProformaInvoice)
            .HasForeignKey(spii => spii.ServiceProformaInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region Bill of Materials

        modelBuilder.Entity<BillOfMaterialItemSubstitute>()
            .HasOne(s => s.BillOfMaterialItem)
            .WithMany(i => i.Substitutes)
            .HasForeignKey(s => s.BillOfMaterialItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BillOfMaterialItemSubstitute>()
            .HasOne(s => s.SubstituteMaterial)
            .WithMany()
            .HasForeignKey(s => s.SubstituteMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region Product Packages

        modelBuilder.Entity<ProductPackageSubstitute>()
            .HasOne(s => s.ProductPackage)
            .WithMany(p => p.Substitutes)
            .HasForeignKey(s => s.ProductPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductPackageSubstitute>()
            .HasOne(s => s.SubstituteMaterial)
            .WithMany()
            .HasForeignKey(s => s.SubstituteMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        // #region Question
        //
        // modelBuilder.Entity<Question>().OwnsOne(f => f.Formula);
        //
    }
}
