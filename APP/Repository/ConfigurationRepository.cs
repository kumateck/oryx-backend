using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Configurations;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductsSampling;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Services;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.WorkOrders;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ConfigurationRepository(ApplicationDbContext context, IMapper mapper) : IConfigurationRepository
{
    public async Task<Result<Guid>> CreateConfiguration(CreateConfigurationRequest request, Guid userId)
    {
        if (await context.Configurations.SingleOrDefaultAsync(c => c.ModelType == request.ModelType) is not null)
        {
            return ConfigurationErrors.ModelTypeNotUnique;
        }
        
        var configuration = mapper.Map<Configuration>(request);
        configuration.CreatedById = userId;
        await context.Configurations.AddAsync(configuration);
        await context.SaveChangesAsync();
        return Result.Success(configuration.Id);
    }

    public async Task<Result<ConfigurationDto>> GetConfiguration(Guid configurationId)
    {
        var configuration = await context.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId);

        return  Result.Success(mapper.Map<ConfigurationDto>(configuration));
    }
    
    public async Task<Result<ConfigurationDto>> GetConfiguration(string modelType)
    {
        var configuration = await context.Configurations
            .FirstOrDefaultAsync(c => c.ModelType == modelType);

        return  Result.Success(mapper.Map<ConfigurationDto>(configuration));
    }

    public async Task<Result<Paginateable<IEnumerable<ConfigurationDto>>>> GetConfigurations(int page, int pageSize, string searchQuery)
    {
        var query = context.Configurations.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Prefix, q => q.ModelType);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ConfigurationDto>);
    }

    public async Task<Result> UpdateConfiguration(CreateConfigurationRequest request, Guid configurationId)
    {
        var existingConfiguration = await context.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId);

        if (existingConfiguration == null)
        {
            return Error.NotFound("Configuration.NotFound", "Configuration is not found");
        }

        mapper.Map(request, existingConfiguration);
        context.Configurations.Update(existingConfiguration);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteConfiguration(Guid configurationId)
    {
        var configuration = await context.Configurations.FindAsync(configurationId);
        if (configuration == null)
        {
            return Error.NotFound("Configuration.NotFound", "Configuration is not found");
        }

        context.Configurations.Remove(configuration);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<int>> GetCountForCodeConfiguration(string modelType, string prefix)
    {

        switch (modelType)
        {
           case "RawMaterial":
               return await context.Materials
                   .Where(m => m.Kind == MaterialKind.Raw && m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case "PackageMaterial":
               return await context.Materials
                   .Where(m => m.Kind == MaterialKind.Package && m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(Product):
               return await context.Products
                   .IgnoreQueryFilters()
                   .Where(m => m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(ProductionSchedule):
               return await context.ProductionSchedules
                   .IgnoreQueryFilters()
                   .Where(m => m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(WorkOrder):
               return await context.WorkOrders
                   .Where(m => m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(Department):
               return await context.Departments
                   .Where(m => m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(Requisition):
               return await context.Requisitions
                   .Where(m => m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case "StockRequisition":
               return await context.Requisitions
                   .IgnoreQueryFilters()
                   .Where(m => m.RequisitionType == RequisitionType.Stock && m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case "PurchaseRequisition":
               return await context.Requisitions
                   .IgnoreQueryFilters()
                   .Where(m => m.RequisitionType == RequisitionType.Purchase && m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(SourceRequisition):
               return await context.SourceRequisitions
                   .IgnoreQueryFilters()
                   .Where(m => m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(ShipmentDocument):
               return await context.ShipmentDocuments
                   .Where(m => m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(PurchaseOrder):
               return await context.PurchaseOrders
                   .IgnoreQueryFilters()
                   .Where(m => m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(Grn):
               return await context.Grns
                   .IgnoreQueryFilters()
                   .Where(m => m.GrnNumber.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(StockTransfer):
               return await context.StockTransfers
                   .IgnoreQueryFilters()
                   .Where(m => m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(OvertimeRequest):
               return await context.OvertimeRequests
                   .IgnoreQueryFilters()
                   .Where(m => m.Code.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(Employee):
               return await context.Employees
                   .IgnoreQueryFilters()
                   .Where(m => m.StaffNumber.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(ProductSampling):
               return await context.ProductSamplings
                   .IgnoreQueryFilters()
                   .Where(m => m.ArNumber.StartsWith(prefix))
                   .CountAsync();
           
           // case nameof(MaterialSampling):
           //     return await context.MaterialSamplings
           //         .IgnoreQueryFilters()
           //         .Where(m => m.ArNumber.StartsWith(prefix))
           //         .CountAsync();
           
           case nameof(FinishedGoodsTransferNote):
               return await context.FinishedGoodsTransferNotes
                   .IgnoreQueryFilters()
                   .CountAsync();

           case "ArNumber":
               return await context.BinCardInformation
                   .IgnoreQueryFilters()
                   .Where(m => m.ArNumber.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(ProductionOrder):
               return await context.ProductionOrders
                   .IgnoreQueryFilters()
                   .Where(po => po.ProductionOrderCode.StartsWith(prefix))
                   .CountAsync();
           
           case nameof(Service):
               return await context.Services
                   .IgnoreQueryFilters()
                   .Where(s => s.Code.StartsWith(prefix))
                   .CountAsync();
           
               
           default:
               return Error.Validation("ModelType", "Invalid model type sent");
        }
    }
}
