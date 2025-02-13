using APP.Mapper.Resolvers;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.Configurations;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Departments.Request;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
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
        CreateMap<MaterialBatchEvent, MaterialBatchEventDto>();
        CreateMap<MaterialBatchMovement, MaterialBatchMovementDto>();

        CreateMap<MaterialCategory, MaterialCategoryDto>();

        #endregion

        #region Requisition

        CreateMap<CreateRequisitionRequest, Requisition>();
        CreateMap<CreateRequisitionItemRequest, RequisitionItem>();
        CreateMap<Requisition, RequisitionDto>();
        CreateMap<RequisitionItem, RequisitionItemDto>();
        CreateMap<RequisitionApproval, RequisitionApprovalDto>();
        CreateMap<CreateRequisitionRequest, CompletedRequisition>();
        CreateMap<CreateRequisitionItemRequest, CompletedRequisitionItem>();
        CreateMap<CompletedRequisition, RequisitionDto>();
        CreateMap<CompletedRequisitionItem, RequisitionItemDto>();
        CreateMap<CreateSourceRequisitionRequest, SourceRequisition>();
        CreateMap<CreateSourceRequisitionItemRequest, SourceRequisitionItem>();
        CreateMap<SourceRequisition, SourceRequisitionDto>(); 
        CreateMap<SourceRequisition, SupplierQuotationRequest>(); 
        CreateMap<SourceRequisitionItem, SourceRequisitionItemDto>();
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
        CreateMap<WarehouseArrivalLocation,WarehouseArrivalLocationDto>();
        CreateMap<DistributedRequisitionMaterial, DistributedRequisitionMaterialDto>();
        CreateMap<CreateArrivalLocationRequest, WarehouseArrivalLocation>();
        CreateMap<UpdateArrivalLocationRequest, WarehouseArrivalLocation>();
        #endregion

        #region Department
        
        CreateMap<CreateDepartmentRequest, Department>();
        CreateMap<CreateDepartmentWarehouseRequest, DepartmentWarehouse>();
        CreateMap<UpdateDepartmentRequest, Department>();
        CreateMap<Department, DepartmentDto>();
        CreateMap<DepartmentWarehouse, DepartmentWarehouseDto>();

        #endregion

        #region Attachment

        CreateMap<SourceRequisition, SourceRequisitionDto>()
            .ForMember(dest => dest.Attachments,
                opt => opt.MapFrom<AttachmentsResolver>());

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
        CreateMap<PurchaseOrderItem, PurchaseOrderItemDto>();

        CreateMap<CreatePurchaseOrderInvoiceRequest, PurchaseOrderInvoice>();
        CreateMap<CreateBatchItemRequest, BatchItem>();
        CreateMap<CreateChargeRequest, Charge>();
        CreateMap<PurchaseOrderInvoice, PurchaseOrderInvoiceDto>();
        CreateMap<BatchItem, BatchItemDto>();
        CreateMap<Charge, ChargeDto>();

        CreateMap<CreateBillingSheetRequest, BillingSheet>();
        CreateMap<BillingSheet, BillingSheetDto>();

        CreateMap<CreatePurchaseOrderRequest, RevisedPurchaseOrder>();
        CreateMap<CreatePurchaseOrderItemRequest, RevisedPurchaseOrderItem>();
        CreateMap<RevisedPurchaseOrder, RevisedPurchaseOrderDto>();
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

        #region Production

        CreateMap<CreateBatchManufacturingRecord, BatchManufacturingRecord>();
        CreateMap<UpdateBatchManufacturingRecord, BatchManufacturingRecord>();
        CreateMap<BatchManufacturingRecord, BatchManufacturingRecordDto>();

        CreateMap<CreateBatchPackagingRecord, BatchPackagingRecord>();
        CreateMap<UpdateBatchPackagingRecord, BatchPackagingRecord>();
        CreateMap<BatchPackagingRecord, BatchPackagingRecordDto>();

        CreateMap<ProductionActivity, ProductionActivityDto>();
        CreateMap<ProductionActivity, ProductionActivityListDto>();
        CreateMap<ProductionActivityLog, ProductionActivityLogDto>();
        CreateMap<ProductionActivityStep, ProductionActivityStepDto>();
        CreateMap<ProductionActivityStepResource, ProductionActivityStepResourceDto>();
        CreateMap<ProductionActivityStepWorkCenter, ProductionActivityStepWorkCenterDto>();
        CreateMap<ProductionActivityStepUser, ProductionActivityStepUserDto>();


        #endregion
    }
}