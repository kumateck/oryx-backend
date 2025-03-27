using APP.Mapper.Resolvers;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Charges;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Configurations;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Departments.Request;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Procurement.Distribution;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.ProductionSchedules.StockTransfers.Request;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Shipments.Request;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Users.Request;
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
        CreateMap<CreateItemRequest, Charge>();
        
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
        CreateMap<ShipmentInvoice, CollectionItemDto>();
        CreateMap<Charge, CollectionItemDto>();
        
        #endregion

        #region UoM

        CreateMap<UnitOfMeasure, UnitOfMeasureDto>();

        #endregion

        #region Country

        CreateMap<Country, CountryDto>();

        #endregion

        #region Resource
        
        CreateMap<Resource, ResourceDto>().ReverseMap();
        #endregion
        
        #region UserMapper
        CreateMap<CreateUserRequest, User>();
        CreateMap<User, UserDto>()
            // .ForMember(user => user.Roles,
            //     opt => opt.MapFrom<UserRoleResolver>())
            .ForMember(user => user.Avatar,
                opt => opt.MapFrom<AvatarResolver>());
        
        #endregion

        #region RoleMapper
        
        CreateMap<CreateRoleRequest, Role>();
        CreateMap<Role, RoleDto>().ReverseMap();
        CreateMap<RoleDto, RolePermissionDto>();
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
                opt => opt.MapFrom(src => src. Batches.Where(b => b.Status == BatchStatus.Available).Sum(b => b.RemainingQuantity)));

        CreateMap<CreateMaterialBatchRequest, MaterialBatch>();
        CreateMap<MaterialBatch, MaterialBatchDto>();
        CreateMap<MaterialBatch, DistributedMaterialBatchDto>();
        CreateMap<MaterialBatchEvent, MaterialBatchEventDto>();
        CreateMap<MassMaterialBatchMovement, MassMaterialBatchMovementDto>();
        CreateMap<MaterialBatchReservedQuantity, MaterialBatchReservedQuantityDto>();

        CreateMap<CreateSrRequest, Sr>();
        CreateMap<Sr, SrDto>();

        CreateMap<MaterialCategory, MaterialCategoryDto>();

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
        CreateMap<CreatePurchaseOrderItemRequest, PurchaseOrderItem>();
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(dest => dest.Attachments,
                opt => opt.MapFrom<AttachmentsResolver>())
            .ForMember(dest => dest.AttachmentStatus,
                opt => opt.MapFrom<PurchaseOrderStatusResolver>());
        CreateMap<PurchaseOrderItem, PurchaseOrderItemDto>()
            .ForMember(dest => dest.CanReassignSupplier,
                opt => opt.MapFrom<CanReassignPurchaseOrderItemResolver>());

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
        CreateMap<Checklist, DistributedChecklistDto>();
        CreateMap<Checklist, BatchChecklistDto>();
        CreateMap<CreateChecklistRequest, Checklist>()
            .ForMember(dest => dest.MaterialBatches,
                opt => opt.Ignore());

        #endregion
        
        #region Grn
        CreateMap<CreateGrnRequest, Grn>();
        CreateMap<Grn, GrnDto>();
        #endregion
        

        #region Form

        CreateMap<CreateFormRequest, Form>();
        CreateMap<CreateFormSectionRequest, FormSection>();
        CreateMap<CreateFormFieldRequest, FormField>();
        CreateMap<CreateFormAssigneeRequest, FormAssignee>();
        CreateMap<CreateFormReviewerRequest, FormReviewer>();

        CreateMap<Form, FormDto>();
        CreateMap<FormSection, FormSectionDto>();
        CreateMap<FormField, FormFieldDto>();
        CreateMap<Response, ResponseDto>();
        CreateMap<FormResponse, FormResponseDto>()
            .ForMember(dest => dest.Attachments,
                opt => opt.MapFrom<AttachmentsResolver>());
        CreateMap<FormAssignee, FormAssigneeDto>();
        CreateMap<FormReviewer, FormReviewerDto>();

        CreateMap<CreateQuestionRequest, Question>();
        CreateMap<CreateQuestionOptionsRequest, QuestionOption>();
        CreateMap<Question, QuestionDto>();
        CreateMap<QuestionOption, QuestionOptionDto>();

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
        CreateMap<FinishedGoodsTransferNote, FinishedGoodsTransferNoteDto>();

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
    }
}