using APP.Mapper.Resolvers;
using AutoMapper;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Charges;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Children;
using DOMAIN.Entities.CompanyWorkingDays;
using DOMAIN.Entities.Configurations;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Departments.Request;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.EducationHistories;
using DOMAIN.Entities.EmergencyContacts;
using DOMAIN.Entities.EmployeeHistories;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Holidays;
using DOMAIN.Entities.Instruments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Invoices;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.ItemStockRequisitions;
using DOMAIN.Entities.LeaveEntitlements;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.LeaveTypes;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSampling;
using DOMAIN.Entities.MaterialSpecifications;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.Persons;
using DOMAIN.Entities.Procurement.Distribution;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductAnalyticalRawData;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.ProductionSchedules.StockTransfers.Request;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductSpecifications;
using DOMAIN.Entities.ProductsSampling;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.ProformaInvoices;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.ServiceProviders;
using DOMAIN.Entities.Services;
using DOMAIN.Entities.ShiftAssignments;
using DOMAIN.Entities.ShiftSchedules;
using DOMAIN.Entities.ShiftTypes;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Shipments.Request;
using DOMAIN.Entities.Siblings;
using DOMAIN.Entities.StaffRequisitions;
using DOMAIN.Entities.UniformityOfWeights;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Users.Request;
using DOMAIN.Entities.VendorQuotations;
using DOMAIN.Entities.Vendors;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Warehouses.Request;
using DOMAIN.Entities.WorkOrders;
using SHARED;

namespace APP.Mapper;

public class OryxMapper : Profile
{
    public OryxMapper()
    {

        #region CreateItemRequest
        
        CreateMap<CreateItemRequest, Resource>();
        CreateMap<CreateItemRequest, ProductCategory>();
        CreateMap<CreateItemRequest, UnitOfMeasure>();
        CreateMap<CreateItemRequest, Material>();
        CreateMap<CreateItemRequest, Operation>();
        CreateMap<CreateItemRequest, WorkCenter>();
        CreateMap<CreateItemRequest, MaterialType>();
        CreateMap<CreateItemRequest, MaterialCategory>();
        CreateMap<CreateItemRequest, PackageType>();
        CreateMap<CreateItemRequest, Currency>();
        CreateMap<CreateItemRequest, ShipmentDiscrepancyType>();
        CreateMap<CreateItemRequest, PackageStyle>();
        CreateMap<CreateItemRequest, TermsOfPayment>();
        CreateMap<CreateItemRequest, DeliveryMode>();
        CreateMap<CreateItemRequest, Charge>();
        CreateMap<CreateItemRequest, ShiftCategory>();
        CreateMap<CreateItemRequest, MarketType>();
        CreateMap<CreateItemRequest, Instrument>();
        CreateMap<CreateItemRequest, ProductState>();
        
        #endregion
        
        #region CollectionItems
        
        CreateMap<ProductCategory, CollectionItemDto>();
        CreateMap<UnitOfMeasure, CollectionItemDto>();
        CreateMap<Product, CollectionItemDto>();
        CreateMap<Operation, CollectionItemDto>();
        CreateMap<WorkCenter, CollectionItemDto>();
        CreateMap<Resource, CollectionItemDto>();
        CreateMap<RouteResource, CollectionItemDto>()
            .IncludeMembers(src => src.Resource)
            .ForMember(dest => dest.Id,
                opt => opt.MapFrom(src => src.ResourceId));
        CreateMap<MaterialType, CollectionItemDto>();
        CreateMap<MaterialCategory, CollectionItemDto>();
        CreateMap<PackageType, CollectionItemDto>();
        CreateMap<Material, CollectionItemDto>();
        CreateMap<User, CollectionItemDto>()
            .ForMember(dest => dest.Name,
                opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
        CreateMap<Role, CollectionItemDto>().ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.DisplayName));
        CreateMap<Supplier, CollectionItemDto>();
        CreateMap<Supplier, CollectionItemDto>();
        CreateMap<Manufacturer, CollectionItemDto>();
        CreateMap<Country, CollectionItemDto>();
        CreateMap<Warehouse, CollectionItemDto>();
        CreateMap<WarehouseLocation, CollectionItemDto>();
        CreateMap<WarehouseLocationRack, CollectionItemDto>();
        CreateMap<WarehouseLocationShelf, CollectionItemDto>();
        CreateMap<MaterialBatch, CollectionItemDto>();
        CreateMap<SourceRequisition, CollectionItemDto>();
        CreateMap<Requisition, CollectionItemDto>();
        CreateMap<Currency, CollectionItemDto>();
        CreateMap<PurchaseOrder, CollectionItemDto>();
        CreateMap<RevisedPurchaseOrder, CollectionItemDto>();
        CreateMap<PurchaseOrderInvoice, CollectionItemDto>();
        CreateMap<BillingSheet, CollectionItemDto>();
        CreateMap<ShipmentDiscrepancyType, CollectionItemDto>();
        CreateMap<Form, CollectionItemDto>();
        CreateMap<FormSection, CollectionItemDto>();
        CreateMap<ProductionSchedule, CollectionItemDto>();
        CreateMap<ProductionActivity, CollectionItemDto>();
        CreateMap<ProductionSchedule, CollectionItemDto>();
        CreateMap<Department, CollectionItemDto>();
        CreateMap<Equipment, CollectionItemDto>();
        CreateMap<PackageStyle, CollectionItemDto>();
        CreateMap<TermsOfPayment, CollectionItemDto>();
        CreateMap<DeliveryMode, CollectionItemDto>();
        CreateMap<ShipmentInvoice, CollectionItemDto>();
        CreateMap<Charge, CollectionItemDto>();
        CreateMap<Question, CollectionItemDto>();
        CreateMap<ShiftCategory, CollectionItemDto>();       
        CreateMap<ProductState, CollectionItemDto>();
        CreateMap<MarketType, CollectionItemDto>();
        CreateMap<Instrument, CollectionItemDto>();
        CreateMap<InventoryPurchaseRequisition, CollectionItemDto>();
        CreateMap<MarketRequisition, CollectionItemDto>();
        CreateMap<Vendor, CollectionItemDto>();
        CreateMap<Item, CollectionItemDto>();
        CreateMap<Customer, CollectionItemDto>();
        
        #endregion

        #region Operation

        CreateMap<Operation, OperationDto>();

        #endregion

        #region UoM

        CreateMap<UnitOfMeasure, UnitOfMeasureDto>();

        #endregion

        #region Country

        CreateMap<Country, CountryDto>();
        CreateMap<CountryDto, CountryDto>();

        #endregion

        #region Resource
        
        CreateMap<Resource, ResourceDto>().ReverseMap();
        #endregion
        
        #region UserMapper
        CreateMap<CreateUserRequest, User>();
        CreateMap<User, UserDto>()
            .ForMember(user => user.Avatar,
                opt => opt.MapFrom<AvatarResolver>())
            .ForMember(user => user.Signature,
                opt => opt.MapFrom<SignatureResolver>());
        
        CreateMap<User, BsonUserDto>();
        CreateMap<BsonUserDto, BsonUserDto>();
        
        CreateMap<User, UserWithRoleDto>()
             .ForMember(user => user.Roles,
                 opt => opt.MapFrom<UserRoleResolver>())
            .ForMember(user => user.Avatar,
                opt => opt.MapFrom<AvatarResolver>())
            .ForMember(user => user.Signature,
                opt => opt.MapFrom<SignatureResolver>());
        
        #endregion

        #region RoleMapper
        
        CreateMap<CreateRoleRequest, Role>();
        CreateMap<Role, RoleDto>().ReverseMap();
        CreateMap<RoleDto, RolePermissionDto>();
        CreateMap<Role, RolePermissionDto>();
        // CreateMap<string, PermissionResponseDto>()
        //     .ForMember(item => item.GroupName,
        //         opt => opt.MapFrom(src => src.GetGroupName()))
        //     .ForMember(item => item.Description,
        //         opt => opt.MapFrom(src => src.FormatPermissionDescriptionFromAction()))
        //     .ForMember(item => item.Action,
        //         opt => opt.MapFrom(src => src));
        
        #endregion

        #region Product
        
        CreateMap<CreateProductRequest, Product>();
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CurrentBillOfMaterial,
                opt => opt.MapFrom<ProductBoMResolver>())
            .ForMember(dest => dest.OutdatedBillOfMaterials,
                opt => opt.MapFrom<OutdatedProductBoMResolver>());
        CreateMap<Product, ProductListDto>();
        CreateMap<CreateProductBillOfMaterialRequest, ProductBillOfMaterial>();
        CreateMap<ProductBillOfMaterial, ProductBillOfMaterialDto>();
        CreateMap<CreateFinishedProductRequest, FinishedProduct>();
        CreateMap<FinishedProduct, FinishedProductDto>();
        CreateMap<CreateProductPackageRequest, ProductPackage>();
        CreateMap<ProductPackage, ProductPackageDto>();

        CreateMap<CreateProductSpecificationRequest, ProductSpecification>();
        CreateMap<ProductSpecification, ProductSpecificationDto>()
            .ForMember(dest => dest.PackingStyle, opt => opt.MapFrom(src => src.Product.PackageStyle))
            .ForMember(dest => dest.LabelClaim, opt => opt.MapFrom(src => src.Product.LabelClaim))
            .ForMember(dest => dest.ShelfLife, opt => opt.MapFrom(src => src.Product.ShelfLife));

        
        #endregion

        #region BoM
        CreateMap<CreateBillOfMaterialRequest, BillOfMaterial>();
        CreateMap<BillOfMaterial, BillOfMaterialDto>();
        CreateMap<CreateBoMItemsRequest, BillOfMaterialItem>();
        CreateMap<BillOfMaterialItem, BillOfMaterialItemDto>();
        #endregion

        #region WorkOrder
        CreateMap<CreateWorkOrderRequest, WorkOrder>();
        CreateMap<WorkOrder, WorkOrderDto>();
        CreateMap<CreateProductionStepRequest, ProductionStep>();
        CreateMap<ProductionStep, ProductionStepDto>();
        #endregion

        #region ProductionSchdule
        CreateMap<CreateProductionScheduleRequest, ProductionSchedule>();
        CreateMap<CreateProductionScheduleItemRequest, ProductionScheduleItem>();
        CreateMap<ProductionSchedule, ProductionScheduleDto>();
        CreateMap<ProductionScheduleItem, ProductionScheduleItemDto>();
        CreateMap<CreateMasterProductionScheduleRequest, MasterProductionSchedule>();
        CreateMap<MasterProductionSchedule, MasterProductionScheduleDto>();
        CreateMap<CreateProductionScheduleProduct, ProductionScheduleProduct>();
        CreateMap<ProductionScheduleProduct, ProductionScheduleProductDto>();

        CreateMap<CreateFinalPacking, FinalPacking>();
        CreateMap<CreateFinalPackingMaterial, FinalPackingMaterial>();

        CreateMap<FinalPacking, FinalPackingDto>();
        CreateMap<FinalPackingMaterial, FinalPackingMaterialDto>();

        CreateMap<ProductionExtraPacking, ProductionExtraPackingDto>();
        CreateMap<ProductionExtraPacking, ProductionExtraPackingWithBatchesDto>();

        #endregion

        #region Route
        CreateMap<CreateRouteRequest, Route>();
        CreateMap<Route, RouteDto>();
        CreateMap<CreateRouteResource, RouteResource>();
        CreateMap<CreateRouteResponsibleUser, RouteResponsibleUser>();
        CreateMap<CreateRouteResponsibleRole, RouteResponsibleRole>();
        CreateMap<CreateRouteWorkCenter, RouteWorkCenter>();
        CreateMap<RouteResponsibleUser, RouteResponsibleUserDto>();
        CreateMap<RouteResponsibleRole, RouteResponsibleRoleDto>();
        CreateMap<RouteWorkCenter, RouteWorkCenterDto>();
        CreateMap<RouteResource, RouteResourceDto>();
        #endregion

        #region Configuration
        CreateMap<CreateConfigurationRequest, Configuration>();
        CreateMap<Configuration, ConfigurationDto>();
        #endregion

        #region Material

        CreateMap<CreateMaterialRequest, Material>();
        CreateMap<Material, MaterialDto>()
            .ForMember(dest => dest.TotalStock,
                opt => opt.MapFrom(src => src.Batches.Where(b => b.Status == BatchStatus.Available).Sum(b => b.RemainingQuantity)));
        
        CreateMap<Material, MaterialWithWarehouseStockDto>()
            .ForMember(dest => dest.TotalStock,
                opt => opt.MapFrom(src => src.Batches.Where(b => b.Status == BatchStatus.Available).Sum(b => b.RemainingQuantity)));

        CreateMap<CreateMaterialBatchRequest, MaterialBatch>();
        CreateMap<MaterialBatch, MaterialBatchDto>();
        CreateMap<MaterialBatch, MaterialBatchListDto>();
        CreateMap<MaterialBatch, DistributedMaterialBatchDto>();
        CreateMap<MaterialBatchEvent, MaterialBatchEventDto>();
        CreateMap<MassMaterialBatchMovement, MassMaterialBatchMovementDto>();
        CreateMap<MaterialBatchReservedQuantity, MaterialBatchReservedQuantityDto>();
        CreateMap<MaterialDepartment, MaterialDepartmentDto>();
        CreateMap<MaterialDepartment, MaterialDepartmentWithWarehouseStockDto>();
            // .ForMember(dest => dest.WarehouseStock,
            //     opt => opt.MapFrom<>());
        
        CreateMap<CreateSrRequest, Sr>();
        CreateMap<Sr, SrDto>();

        CreateMap<MaterialCategory, MaterialCategoryDto>();
        
        CreateMap<MaterialReturnNote, MaterialReturnNoteDto>();
        CreateMap<MaterialReturnNoteFullReturn, MaterialReturnNoteFullReturnDto>();
        CreateMap<MaterialReturnNotePartialReturn, MaterialReturnNotePartialReturnDto>();
        
        CreateMap<HoldingMaterialTransfer, HoldingMaterialTransferDto>();
        CreateMap<HoldingMaterialTransferBatch, HoldingMaterialTransferBatchDto>();

        CreateMap<CreateMaterialSpecificationRequest, MaterialSpecification>();
        CreateMap<MaterialSpecification, MaterialSpecificationDto>();
            
        #endregion

        #region Requisition

        CreateMap<CreateRequisitionRequest, Requisition>();
        CreateMap<CreateRequisitionItemRequest, RequisitionItem>();
        CreateMap<Requisition, RequisitionDto>();
        CreateMap<RequisitionItem, RequisitionItemDto>();
        CreateMap<RequisitionApproval, RequisitionApprovalDto>();
        // CreateMap<CreateRequisitionRequest, CompletedRequisition>();
        // CreateMap<CreateRequisitionItemRequest, CompletedRequisitionItem>();
        // CreateMap<CompletedRequisition, RequisitionDto>();
        // CreateMap<CompletedRequisitionItem, RequisitionItemDto>();
        CreateMap<CreateSourceRequisitionRequest, SourceRequisition>();
        CreateMap<CreateSourceRequisitionItemRequest, SourceRequisitionItem>();
        CreateMap<SourceRequisitionItem, SourceRequisitionItemDto>();
        CreateMap<SourceRequisition, SourceRequisitionDto>()
            .ForMember(dest => dest.Attachments,
                opt => opt.MapFrom<AttachmentsResolver>())
            .AfterMap((src, dest, context) =>
            {
                // Manually handle grouping inside AfterMap
                var groupedItems = src.Items
                    .GroupBy(i => i.Material.Id)
                    .Select(g => new SourceRequisitionItemDto
                    {
                        Id = g.First().Id,  // Keep the first ID (arbitrary, can be changed)
                        SourceRequisition = context.Mapper.Map<CollectionItemDto>(g.First().SourceRequisition),
                        Material = context.Mapper.Map<MaterialDto>(g.First().Material),
                        UoM = context.Mapper.Map<UnitOfMeasureDto>(g.First().UoM),
                        Quantity = g.Sum(i => i.Quantity), // Sum quantities
                        Source = g.First().Source,
                        CreatedAt = g.First().CreatedAt
                    })
                    .ToList();

                // Assign the grouped list to the DTO
                dest.Items = groupedItems;
            });
        CreateMap<SourceRequisition, SupplierQuotationRequest>()
            .AfterMap((src, dest, context) =>
            {
                // Manually handle grouping inside AfterMap
                var groupedItems = src.Items
                    .GroupBy(i => i.Material.Id)
                    .Select(g => new SourceRequisitionItemDto
                    {
                        Id = g.First().Id,  // Keep the first ID (arbitrary, can be changed)
                        SourceRequisition = context.Mapper.Map<CollectionItemDto>(g.First().SourceRequisition),
                        Material = context.Mapper.Map<MaterialDto>(g.First().Material),
                        UoM = context.Mapper.Map<UnitOfMeasureDto>(g.First().UoM),
                        Quantity = g.Sum(i => i.Quantity), // Sum quantities
                        Source = g.First().Source,
                        CreatedAt = g.First().CreatedAt
                    })
                    .ToList();

                // Assign the grouped list to the DTO
                dest.Items = groupedItems;
            });
        #endregion

        #region Approvals

        CreateMap<CreateApprovalRequest, Approval>();
        CreateMap<CreateApprovalStageRequest, ApprovalStage>();
        CreateMap<Approval, ApprovalDto>();
        CreateMap<ApprovalStage, ApprovalStageDto>();

        #endregion

        #region Procurement
        
        //supplier
        CreateMap<CreateSupplierRequest, Supplier>();
        CreateMap<Supplier, SupplierDto>();
        CreateMap<CreateSupplierManufacturerRequest, SupplierManufacturer>();
        CreateMap<SupplierManufacturer, SupplierManufacturerDto>();
        
        //manufacturer
        CreateMap<CreateManufacturerRequest, Manufacturer>();
        CreateMap<Manufacturer, ManufacturerDto>();
        CreateMap<CreateManufacturerMaterialRequest, ManufacturerMaterial>();
        CreateMap<ManufacturerMaterial, ManufacturerMaterialDto>();
        
        //distribution

        #endregion

        #region Warehoues

        CreateMap<CreateWarehouseRequest, Warehouse>();
        CreateMap<UpdateWarehouseRequest, Warehouse>();
        CreateMap<CreateWarehouseLocationRequest, WarehouseLocation>();
        CreateMap<CreateWarehouseLocationRackRequest, WarehouseLocationRack>();
        CreateMap<CreateWarehouseLocationShelfRequest, WarehouseLocationShelf>();
        CreateMap<Warehouse, WarehouseDto>();
        CreateMap<WarehouseLocation, WarehouseLocationDto>();
        CreateMap<WarehouseLocation, WareHouseLocationDto>();
        CreateMap<WarehouseLocationRack, WarehouseLocationRackDto>();
        CreateMap<WarehouseLocationRack, WareHouseLocationRackDto>();
        CreateMap<WarehouseLocationShelf, WarehouseLocationShelfDto>();
        CreateMap<WarehouseLocationShelf,MaterialWarehouseLocationShelfDto>();
        CreateMap<WarehouseArrivalLocation,WarehouseArrivalLocationDto>();
        CreateMap<DistributedRequisitionMaterial, DistributedRequisitionMaterialDto>();
        CreateMap<DistributedFinishedProduct, DistributedFinishedProductDto>();
        CreateMap<CreateArrivalLocationRequest, WarehouseArrivalLocation>();
        CreateMap<UpdateArrivalLocationRequest, WarehouseArrivalLocation>();
        CreateMap<MaterialItemDistribution, MaterialItemDistributionDto>();
        CreateMap<Warehouse, WarehouseWithoutLocationDto>();
        #endregion

        #region BinCardInformation

        CreateMap<BinCardInformation, BinCardInformationDto>();
        CreateMap<ProductBinCardInformation, ProductBinCardInformationDto>();

        #endregion

        #region Department
        
        CreateMap<CreateDepartmentRequest, Department>();
        CreateMap<UpdateDepartmentRequest, Department>();
        CreateMap<Department, DepartmentDto>();

        #endregion

        #region Currency

        CreateMap<Currency, CurrencyDto>();

        #endregion

        #region Supplier Quotation

        CreateMap<SupplierQuotation, SupplierQuotationDto>();
        CreateMap<SupplierQuotationItem, SupplierQuotationItemDto>();

        #endregion

        #region Purchase Order
        
        CreateMap<CreatePurchaseOrderRequest, PurchaseOrder>();
        CreateMap<UpdatePurchaseOrderRequest, PurchaseOrder>();
        CreateMap<CreatePurchaseOrderItemRequest, PurchaseOrderItem>();
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(dest => dest.Attachments,
                opt => opt.MapFrom<AttachmentsResolver>())
            .ForMember(dest => dest.AttachmentStatus,
                opt => opt.MapFrom<PurchaseOrderStatusResolver>())
            .ForMember(dest => dest.Revisions,
                opt => opt.MapFrom<PurchaseOrderRevisionResolver>())
            .ForMember(dest => dest.Items,
                opt => opt.MapFrom(src =>
                    src.Items.Where(item => item.DeletedAt == null)));
        
        CreateMap<PurchaseOrderItem, PurchaseOrderItemDto>()
            .ForMember(dest => dest.CanReassignSupplier,
                opt => opt.MapFrom<CanReassignPurchaseOrderItemResolver>());
        CreateMap<PurchaseOrderItemSnapshot, PurchaseOrderItemDto>();

        CreateMap<CreatePurchaseOrderInvoiceRequest, PurchaseOrderInvoice>();
        CreateMap<CreateBatchItemRequest, BatchItem>();
        CreateMap<CreatePurchaseOrderChargeRequest, PurchaseOrderCharge>();
        CreateMap<PurchaseOrderInvoice, PurchaseOrderInvoiceDto>();
        CreateMap<BatchItem, BatchItemDto>();
        CreateMap<PurchaseOrderCharge, PurchaseOrderChargeDto>();

        CreateMap<Charge,ChargeDto>();
        CreateMap<CreateChargeRequest, Charge>();
        CreateMap<CreateBillingSheetRequest, BillingSheet>()
            .ForMember(dest => dest.Charges, opt => opt.MapFrom<AssignChargesResolver>());
        
        CreateMap<BillingSheet, BillingSheetDto>();

        CreateMap<CreatePurchaseOrderRequest, RevisedPurchaseOrder>();
        CreateMap<CreatePurchaseOrderItemRequest, RevisedPurchaseOrderItem>();
        CreateMap<RevisedPurchaseOrder, RevisedPurchaseOrderDto>();
        CreateMap<CreatePurchaseOrderRevision, RevisedPurchaseOrder>();
        CreateMap<RevisedPurchaseOrderItem, RevisedPurchaseOrderItemDto>();

        #endregion

        #region Shipment Document

        CreateMap<CreateShipmentDocumentRequest, ShipmentDocument>();
        CreateMap<ShipmentDocument, ShipmentDocumentDto>()
            .ForMember(dest => dest.Attachments,
                opt => opt.MapFrom<AttachmentsResolver>());

        CreateMap<CreateShipmentInvoice, ShipmentInvoice>();
        CreateMap<CreateShipmentInvoiceItem, ShipmentInvoiceItem>();
        CreateMap<ShipmentInvoice, ShipmentInvoiceDto>();
        CreateMap<ShipmentInvoiceItem, ShipmentInvoiceItemDto>()
            .ForMember(dest => dest.Price,
                opt => opt.MapFrom(src => src.PurchaseOrder.Items.First(i => i.MaterialId == src.MaterialId).Price));

        CreateMap<CreateShipmentDiscrepancy, ShipmentDiscrepancy>();
        CreateMap<CreateShipmentDiscrepancyItem, ShipmentDiscrepancyItem>();
        CreateMap<ShipmentDiscrepancy, ShipmentDiscrepancyDto>();
        CreateMap<ShipmentDiscrepancyItem, ShipmentDiscrepancyItemDto>();

        #endregion
        
        #region Checklist

        CreateMap<Checklist, ChecklistDto>();
        CreateMap<Checklist, MaterialBatchChecklistDto>();
        CreateMap<Checklist, DistributedChecklistDto>();
        CreateMap<Checklist, BatchChecklistDto>();
        CreateMap<CreateChecklistRequest, Checklist>()
            .ForMember(dest => dest.MaterialBatches,
                opt => opt.Ignore());

        #endregion
        
        #region Grn
        CreateMap<CreateGrnRequest, Grn>();
        CreateMap<Grn, GrnDto>();
        CreateMap<Grn, GrnListDto>();
        #endregion
        

        #region Form

        CreateMap<CreateFormRequest, Form>();
        CreateMap<CreateFormSectionRequest, FormSection>();
        CreateMap<CreateFormFieldRequest, FormField>();
        CreateMap<CreateFormAssigneeRequest, FormAssignee>();
        CreateMap<CreateFormReviewerRequest, FormReviewer>();

        CreateMap<Form, FormDto>()
            .ForMember(dest => dest.Responses,
                opt => opt.MapFrom<FormWithResponseAttachmentResolver>());
        CreateMap<FormSection, FormSectionDto>();
        CreateMap<FormField, FormFieldDto>();
        CreateMap<Response, ResponseDto>()
            .ForMember(dest => dest.FormResponses,
                opt => opt.MapFrom<FormResponseAttachmentResolver>());
        CreateMap<FormResponse, FormResponseDto>()
            .ForMember(dest => dest.Attachments,
                opt => opt.MapFrom<AttachmentsResolver>())
            .ForMember(dest => dest.CheckedBy,
                opt => opt.MapFrom(src => src.Response.CheckedBy))
            .ForMember(dest => dest.CheckedAt,
                opt => opt.MapFrom(src => src.Response.CheckedAt));
        CreateMap<CreateFormResponseRequest, FormResponse>();
        CreateMap<FormAssignee, FormAssigneeDto>();
        CreateMap<FormReviewer, FormReviewerDto>();

        CreateMap<CreateQuestionRequest, Question>();
        CreateMap<CreateQuestionOptionsRequest, QuestionOption>();
        CreateMap<Question, QuestionDto>();
        CreateMap<QuestionOption, QuestionOptionDto>();
        CreateMap<Formula, FormulaDto>();
        CreateMap<FormulaDto, Formula>();

        #endregion

        #region ShelfMaterialBatch

        CreateMap<CreateShelfMaterialBatch, ShelfMaterialBatch>();
        CreateMap<ShelfMaterialBatch, ShelfMaterialBatchDto>();

        #endregion

        #region Production

        CreateMap<CreateBatchManufacturingRecord, BatchManufacturingRecord>();
        CreateMap<UpdateBatchManufacturingRecord, BatchManufacturingRecord>();
        CreateMap<BatchManufacturingRecord, BatchManufacturingRecordDto>();

        CreateMap<CreateBatchPackagingRecord, BatchPackagingRecord>();
        CreateMap<UpdateBatchPackagingRecord, BatchPackagingRecord>();
        CreateMap<BatchPackagingRecord, BatchPackagingRecordDto>();

        CreateMap<CreateFinishedGoodsTransferNoteRequest, FinishedGoodsTransferNote>();
        CreateMap<PackageStyle, PackageStyleDto>();
        CreateMap<FinishedGoodsTransferNote, FinishedGoodsTransferNoteDto>()
            .ForMember(dest => dest.PackageStyle, opt => opt.MapFrom(src => src.PackageStyle));

        CreateMap<ProductionActivity, ProductionActivityDto>();
        CreateMap<ProductionActivity, ProductionActivityListDto>();
        CreateMap<ProductionActivityLog, ProductionActivityLogDto>();
        CreateMap<ProductionActivityStep, ProductionActivityStepDto>();
        CreateMap<ProductionActivityStepResource, ProductionActivityStepResourceDto>();
        CreateMap<ProductionActivityStepWorkCenter, ProductionActivityStepWorkCenterDto>();
        CreateMap<ProductionActivityStepUser, ProductionActivityStepUserDto>();


        CreateMap<CreateStockTransferRequest, StockTransfer>();
        CreateMap<StockTransferSourceRequest, StockTransferSource>();
        CreateMap<StockTransfer, StockTransferDto>();
        CreateMap<StockTransfer, MaterialBatchStockTransferDto>();
        CreateMap<StockTransferSource, StockTransferSourceDto>();
        CreateMap<StockTransferSource, MaterialBatchStockTransferSourceDto>();
        CreateMap<StockTransferSource, DepartmentStockTransferDto>()
            .ForMember(dest => dest.Material,
                opt => opt.MapFrom(src => src.StockTransfer.Material))
            .ForMember(dest => dest.UoM,
                opt => opt.MapFrom(src => src.StockTransfer.UoM))
            .ForMember(dest => dest.Reason,
                opt => opt.MapFrom(src => src.StockTransfer.Reason));

        #endregion

        #region Equipment

        CreateMap<CreateEquipmentRequest, Equipment>();
        CreateMap<Equipment, EquipmentDto>();

        #endregion
        
        #region Employee

        CreateMap<CreateEmployeeRequest, Employee>()
            .ForMember(dest => dest.Mother, opt => opt.MapFrom(src => src.Mother))
            .ForMember(dest => dest.Father, opt => opt.MapFrom(src => src.Father))
            .ForMember(dest => dest.Spouse, opt => opt.MapFrom(src => src.Spouse))
            .ForMember(dest => dest.EmergencyContact, opt => opt.MapFrom(src => src.EmergencyContact))
            .ForMember(dest => dest.NextOfKin, opt => opt.MapFrom(src => src.NextOfKin))
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children))
            .ForMember(dest => dest.Siblings, opt => opt.MapFrom(src => src.Siblings))
            .ForMember(dest => dest.EducationBackground, opt => opt.MapFrom(src => src.EducationBackground))
            .ForMember(dest => dest.EmploymentHistory, opt => opt.MapFrom(src => src.EmploymentHistory));
        
        CreateMap<UpdateEmployeeRequest, Employee>()
            .ForMember(dest => dest.Mother, opt => opt.MapFrom(src => src.Mother))
            .ForMember(dest => dest.Father, opt => opt.MapFrom(src => src.Father))
            .ForMember(dest => dest.Spouse, opt => opt.MapFrom(src => src.Spouse))
            .ForMember(dest => dest.EmergencyContact, opt => opt.MapFrom(src => src.EmergencyContact))
            .ForMember(dest => dest.NextOfKin, opt => opt.MapFrom(src => src.NextOfKin))
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children))
            .ForMember(dest => dest.Siblings, opt => opt.MapFrom(src => src.Siblings))
            .ForMember(dest => dest.EducationBackground, opt => opt.MapFrom(src => src.EducationBackground))
            .ForMember(dest => dest.EmploymentHistory, opt => opt.MapFrom(src => src.EmploymentHistory));

        CreateMap<AssignEmployeeDto, Employee>()
            .ForMember(dest => dest.StaffNumber, opt => opt.MapFrom(src => src.StaffNumber))
            .ForMember(dest => dest.Department, opt => opt.Ignore())
            .ForMember(dest => dest.Designation, opt => opt.Ignore());

        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.Avatar, opt => opt.MapFrom<EmployeeAvatarResolver>());

        CreateMap<EmployeeUserDto, Employee>();

        CreateMap<Employee, User>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));

        CreateMap<Employee, MinimalEmployeeInfoDto>()
            .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Designation, opt => opt.MapFrom(src => src.Designation.Name))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department.Name));

        CreateMap<UpdateEmployeeStatus, Employee>();
        #endregion

        #region Children
        CreateMap<Child, ChildDto>().ReverseMap();

        #endregion

        #region Siblings

        CreateMap<Sibling, SiblingDto>().ReverseMap();

        #endregion

        #region Persons

        CreateMap<Person, PersonDto>().ReverseMap();

        #endregion

        #region Emergency Contact

        CreateMap<EmergencyContact, EmergencyContactDto>().ReverseMap();

        #endregion
        
        #region Education

        CreateMap<Education, EducationDto>().ReverseMap();

        #endregion

        #region Employment

        CreateMap<EmploymentHistory, EmploymentHistoryDto>().ReverseMap();

        #endregion

        #region Leave Type

        CreateMap<LeaveType, LeaveTypeDto>();
        CreateMap<CreateLeaveTypeRequest, LeaveType>();

        #endregion

        #region Leave Requests

        CreateMap<CreateLeaveRequest, LeaveRequest>();
        CreateMap<LeaveRequestDto, LeaveRequest>();
        CreateMap<LeaveRequest, LeaveRequestDto>()
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom<AttachmentsResolver>());
        #endregion

        #region Shift Types

        CreateMap<CreateShiftTypeRequest, ShiftType>();
        CreateMap<ShiftTypeDto, ShiftType>().ReverseMap();

        #endregion

        #region Company Working Days

        CreateMap<CompanyWorkingDaysRequest, CompanyWorkingDays>();
        CreateMap<CompanyWorkingDaysDto, CompanyWorkingDays>().ReverseMap();

        #endregion

        #region Holidays

        CreateMap<CreateHolidayRequest, Holiday>();
        CreateMap<HolidayDto, Holiday>().ReverseMap();
            
        #endregion

        #region Shift Schedules

        CreateMap<ShiftScheduleDto, ShiftSchedule>();
        CreateMap<ShiftSchedule, ShiftScheduleDto>()
            .ForMember(dest => dest.ShiftType, opts => opts.MapFrom(src => src.ShiftTypes));
        CreateMap<CreateShiftScheduleRequest, ShiftSchedule>()
            .ForMember(dest => dest.ShiftTypes, opts => opts.Ignore());
        
        #endregion

        #region Shift Assignments

        CreateMap<ShiftAssignment, ShiftAssignmentDto>();
        
        CreateMap<ShiftAssignmentDto, ShiftAssignment>()
            .ForMember(dest => dest.ShiftTypeId, opt => opt.Ignore());
        
        #endregion
        
        #region ActivityLog

        CreateMap<CreateActivityLog, ActivityLog>();
        CreateMap<ActivityLog, ActivityLogDto>();

        #endregion

        #region Overtime Requests

        CreateMap<CreateOvertimeRequest, OvertimeRequest>();
        CreateMap<OvertimeRequestDto, OvertimeRequest>().ReverseMap();

        #endregion
        
        #region Designations

        CreateMap<CreateDesignationRequest, Designation>();
        CreateMap<Designation, DesignationDto>();
        #endregion
        
        #region Leave Entitlements

        CreateMap<LeaveEntitlementDto, LeaveEntitlement>();
        CreateMap<CreateLeaveEntitlementRequest, LeaveEntitlement>();

        #endregion

        #region Overtime Requests

        CreateMap<CreateOvertimeRequest, OvertimeRequest>()
            .ForMember(dest => dest.Employees, opt => opt.Ignore());
        CreateMap<OvertimeRequestDto, OvertimeRequest>()
            .ForMember(dest => dest.Employees, opt => opt.MapFrom(src => src.Employees));
        #endregion
        
        #region Material Standard Test Procedures

        CreateMap<CreateMaterialStandardTestProcedureRequest, MaterialStandardTestProcedure>();
        
        CreateMap<MaterialStandardTestProcedureDto, MaterialStandardTestProcedure>();
        
        CreateMap<MaterialStandardTestProcedure, MaterialStandardTestProcedureDto>()
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom<AttachmentsResolver>());

        #endregion

        #region Material Analytical Raw Data

        CreateMap<CreateMaterialAnalyticalRawDataRequest, MaterialAnalyticalRawData>();
        CreateMap<MaterialAnalyticalRawDataDto, MaterialAnalyticalRawData>()
            .ForMember(dest => dest.MaterialStandardTestProcedure, opt => opt.Ignore());

        CreateMap<MaterialAnalyticalRawData, MaterialAnalyticalRawDataDto>()
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom<AttachmentsResolver>());

        #endregion

        #region Product Analytical Raw Data
        CreateMap<CreateProductAnalyticalRawDataRequest, ProductAnalyticalRawData>();
        CreateMap<ProductAnalyticalRawDataDto, ProductAnalyticalRawData>()
            .ForMember(dest => dest.ProductStandardTestProcedure, opt => opt.Ignore());

        CreateMap<ProductAnalyticalRawData, ProductAnalyticalRawDataDto>()
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom<AttachmentsResolver>());
        

        #endregion

        #region Product Standard Test Procedures

        CreateMap<CreateProductStandardTestProcedureRequest, ProductStandardTestProcedure>();
        CreateMap<ProductStandardTestProcedureDto, ProductStandardTestProcedure>();
        CreateMap<ProductStandardTestProcedure, ProductStandardTestProcedureDto>()
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom<AttachmentsResolver>());

        #endregion

        #region Staff Requisitions

        CreateMap<CreateStaffRequisitionRequest, StaffRequisition>();
        CreateMap<StaffRequisitionDto, StaffRequisition>().ReverseMap();

        #endregion

        #region AnalyticalTestRequests

        CreateMap<CreateAnalyticalTestRequest, AnalyticalTestRequest>();
        CreateMap<AnalyticalTestRequest, AnalyticalTestRequestDto>();

        #endregion

        #region Alerts

        CreateMap<CreateAlertRequest, Alert>();
        CreateMap<Alert, AlertDto>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles.Select(r => r.Role).ToList()))
            .ForMember(dest => dest.Users, opt => opt.MapFrom(src => src.Users.Select(r => r.User).ToList()));
        #endregion
        
        #region Product Sampling
        
        CreateMap<CreateProductSamplingRequest, ProductSampling>();
        CreateMap<ProductSampling, ProductSamplingDto>().ForMember(dest => dest.AnalyticalTestRequest, opt => opt.MapFrom(src => src.AnalyticalTestRequest));

        #endregion

        #region Material Sampling
        
        CreateMap<CreateMaterialSamplingRequest, MaterialSampling>();
        CreateMap<MaterialSampling, MaterialSamplingDto>().ForMember(dest => dest.GrnDto, opt => opt.MapFrom(src => src.Grn));

        #endregion

        #region Customers

        CreateMap<CreateCustomerRequest, Customer>();
        CreateMap<Customer, CustomerDto>();
        
        #endregion
        
        #region Production Orders
        CreateMap<CreateProductionOrderRequest, ProductionOrder>();
        CreateMap<CreateProductionOrderProduct, ProductionOrderProducts>();
        CreateMap<ProductionOrder, ProductionOrderDto>();
        CreateMap<ProductionOrderProducts, ProductionOrderProductsDto>();

        #endregion

        #region Uniformity Of Weight

        CreateMap<CreateUniformityOfWeight, UniformityOfWeight>();
        CreateMap<UniformityOfWeight, UniformityOfWeightDto>();

        CreateMap<CreateUniformityOfWeightResponse, UniformityOfWeightResponse>();
        CreateMap<UniformityOfWeightResponse, UniformityOfWeightResponseDto>();

        #endregion

        #region Services

        CreateMap<CreateServiceRequest, Service>();
        CreateMap<Service, ServiceDto>()
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom<AttachmentsResolver>());

        #endregion

        #region Service Providers

        CreateMap<CreateServiceProviderRequest, ServiceProvider>();
        CreateMap<ServiceProvider, ServiceProviderDto>();

        #endregion

        #region Vendors

        CreateMap<CreateVendorRequest, Vendor>();
        CreateMap<Vendor, VendorDto>()
            .ForMember(dest => dest.Items, 
                opt =>
                    opt.MapFrom(src => src.Items.Select(i => i.Item).ToList()));

        #endregion

        #region Items

        CreateMap<CreateItemsRequest, Item>();
        CreateMap<Item, ItemDto>()
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom<AttachmentsResolver>());

        #endregion

        #region ProformaInvoice

        CreateMap<CreateProformaInvoice, ProformaInvoice>();
        CreateMap<CreateProformaInvoiceProduct, ProformaInvoiceProduct>();
        CreateMap<ProformaInvoice, ProformaInvoiceDto>();
        CreateMap<ProformaInvoiceProduct, ProformaInvoiceProductDto>();

        #endregion

        #region Invoice

        CreateMap<CreateInvoice, Invoice>();
        CreateMap<Invoice, InvoiceDto>();

        #endregion

        #region Item Stock Requisitions
        
        CreateMap<ItemStockRequisition, ItemStockRequisitionDto>();
        CreateMap<CreateItemStockRequisitionRequest, ItemStockRequisition>()
            .ForMember(dest => dest.RequisitionItems, opt => opt.Ignore());
        
        CreateMap<StockItems, ItemStockRequisitionItem>()
            .ForMember(dest => dest.ItemStockRequisitionId, opt => opt.Ignore())
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId))
            .ForMember(dest => dest.QuantityRequested, opt => opt.MapFrom(src => src.QuantityRequested));

        CreateMap<IssueItemStockRequisition, IssueItemStockRequisitionDto>();
        #endregion

        #region Inventory Procurement

        CreateMap<CreateInventoryPurchaseRequisition, InventoryPurchaseRequisition>();
        CreateMap<CreateInventoryPurchaseRequisitionItem, InventoryPurchaseRequisitionItem>();

        CreateMap<InventoryPurchaseRequisition, InventoryPurchaseRequisitionDto>();
        CreateMap<InventoryPurchaseRequisitionItem, InventoryPurchaseRequisitionItemDto>();


        CreateMap<CreateMarketRequisition, MarketRequisition>();
        CreateMap<MarketRequisition, MarketRequisitionDto>();

        CreateMap<SourceInventoryRequisition, SourceInventoryRequisitionDto>();
        CreateMap<CreateSourceInventoryRequisitionItem, SourceInventoryRequisitionItemDto>();
        CreateMap<SourceInventoryRequisition, VendorQuotationRequest>();

        CreateMap<CreateMarketRequisitionVendor, MarketRequisitionVendor>();
        CreateMap<VendorQuotation, VendorQuotationDto>();
        CreateMap<VendorQuotationItem, VendorQuotationItemDto>();

        CreateMap<MarketRequisitionVendor, MarketRequisitionVendorDto>();

        #endregion
    }
}