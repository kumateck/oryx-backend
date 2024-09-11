using APP.Mapper.Resolvers;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Users;
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

        #endregion

        #region MyRegion

        CreateMap<Resource, ResourceDto>().ReverseMap();

        #endregion
        
        #region UserMapper
        CreateMap<CreateUserRequest, User>();
        CreateMap<User, UserDto>()
            .ForMember(user => user.Roles,
                opt => opt.MapFrom<UserRoleResolver>())
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
        CreateMap<Product, ProductDto>();
        CreateMap<CreateProductBillOfMaterialRequest, ProductBillOfMaterial>();
        CreateMap<ProductBillOfMaterial, ProductBillOfMaterialDto>();

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
        CreateMap<ProductionSchedule, ProductionScheduleDto>();
        CreateMap<CreateMasterProductionScheduleRequest, MasterProductionSchedule>();
        CreateMap<MasterProductionSchedule, MasterProductionScheduleDto>();

        #endregion

        #region Route

        CreateMap<CreateRouteRequest, Route>();
        CreateMap<Route, RouteDto>();

        #endregion

    }
}