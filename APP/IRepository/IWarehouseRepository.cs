using APP.Utils;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Warehouses.Request;
using SHARED;

namespace APP.IRepository;

public interface IWarehouseRepository
{
    Task<Result<Guid>> CreateWarehouse(CreateWarehouseRequest request);
    Task<Result<WarehouseDto>> GetWarehouse(Guid warehouseId);
    Task<Result<Paginateable<IEnumerable<WarehouseDto>>>> GetWarehouses(int page, int pageSize, string searchQuery, WarehouseType? type);
    Task<Result> UpdateWarehouse(CreateWarehouseRequest request, Guid warehouseId, Guid userId);
    Task<Result> DeleteWarehouse(Guid warehouseId, Guid userId);
    Task<Result<Guid>> CreateWarehouseLocation(CreateWarehouseLocationRequest request, Guid warehouseId,
        Guid userId);
    Task<Result<WarehouseLocationRackDto>> GetWarehouseLocation(Guid locationId);
    Task<Result<Paginateable<IEnumerable<WarehouseLocationDto>>>> GetWarehouseLocations(int page,
        int pageSize, string searchQuery);
    Task<Result<List<WarehouseLocationDto>>> GetWarehouseLocations();
    Task<Result> UpdateWarehouseLocation(CreateWarehouseLocationRequest request, Guid locationId,
        Guid userId);
    Task<Result> DeleteWarehouseLocation(Guid locationId, Guid userId);
    Task<Result<Guid>> CreateWarehouseLocationRack(CreateWarehouseLocationRackRequest request,
        Guid warehouseLocationId, Guid userId);
    Task<Result<WarehouseLocationRackDto>> GetWarehouseLocationRack(Guid rackId);
    Task<Result<Paginateable<IEnumerable<WarehouseLocationRackDto>>>> GetWarehouseLocationRacks(int page,
        int pageSize, string searchQuery, MaterialKind? kind);
    Task<Result<List<WarehouseLocationRackDto>>> GetWarehouseLocationRacks(MaterialKind kind, Guid userId);
    Task<Result> UpdateWarehouseLocationRack(CreateWarehouseLocationRackRequest request, Guid rackId,
        Guid userId); 
    Task<Result> DeleteWarehouseLocationRack(Guid rackId, Guid userId);
    Task<Result<Guid>> CreateWarehouseLocationShelf(CreateWarehouseLocationShelfRequest request,
        Guid warehouseLocationRackId, Guid userId);
    Task<Result<WarehouseLocationShelfDto>> GetWarehouseLocationShelf(Guid shelfId);
    Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetWarehouseLocationShelves(
        int page, int pageSize, string searchQuery);
    Task<Result<List<WarehouseLocationShelfDto>>> GetWarehouseLocationShelves(MaterialKind kind,
        Guid userId);
    Task<Result> UpdateWarehouseLocationShelf(CreateWarehouseLocationShelfRequest request, Guid shelfId,
        Guid userId);
    Task<Result> DeleteWarehouseLocationShelf(Guid shelfId, Guid userId);
    Task<Result<WarehouseArrivalLocationDto>> GetArrivalLocationDetails(Guid warehouseId);
    Task<Result<DistributedRequisitionMaterialDto>> GetDistributedRequisitionMaterialById(Guid id);
    Task<Result> UpdateArrivalLocation(UpdateArrivalLocationRequest request);
    Task<Result<Guid>> CreateArrivalLocation(CreateArrivalLocationRequest request);
    Task<Result> ConfirmArrival(Guid distributedMaterialId);
    Task<Result<ChecklistDto>> GetChecklist(Guid id);
    Task<Result<Guid>> CreateChecklist(CreateChecklistRequest request,Guid userId);
    Task<Result<List<MaterialBatchDto>>> GetMaterialBatchByDistributedMaterial(Guid distributedMaterialId);
    Task<Result<List<MaterialBatchDto>>> GetMaterialBatchByDistributedMaterials(List<Guid> distributedMaterialIds);
    Task<Result<ChecklistDto>> GetChecklistByDistributedMaterialId(Guid distributedMaterialId);
    Task<Result<Guid>> CreateGrn(CreateGrnRequest request, List<Guid> materialBatchIds);
    Task<Result<GrnDto>> GetGrn(Guid id);
    Task<Result<Paginateable<IEnumerable<GrnListDto>>>> GetGrns(int page, int pageSize, string searchQuery, MaterialKind? kind);

    Task<Result<Paginateable<IEnumerable<BinCardInformationDto>>>> GetBinCardInformation(int page, int pageSize,
        string searchQuery, Guid materialId);
    
    Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetShelvesByMaterialId(int page, int pageSize, string searchQuery,Guid warehouseId, Guid materialId);
    Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetShelvesByMaterialBatchId(int page, int pageSize, string searchQuery,Guid warehouseId, Guid materialBatchId);

    Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetAllShelves(int page, int pageSize,
        string searchQuery, Guid warehouseId);
    Task<Result<Paginateable<IEnumerable<DistributedRequisitionMaterialDto>>>> GetDistributedRequisitionMaterials(int page, int pageSize, string searchQuery, MaterialKind kind, Guid userId);
    Task<Result<Paginateable<IEnumerable<MaterialBatchDto>>>>GetStockTransferDetails(int page, int pageSize, string searchQuery, MaterialKind kind,Guid userId);
    Task<Result<Paginateable<IEnumerable<DistributedFinishedProductDto>>>> GetFinishedGoodsDetails(int page, int pageSize, string searchQuery, Guid userId);
    Task<Result<DistributedRequisitionMaterialDto>> GetDistributedRequisitionMaterialsById(
        Guid distributedMaterialId);

    Task<Result<Paginateable<IEnumerable<WarehouseLocationShelfDto>>>> GetShelvesByRackId(int page, int pageSize, string searchQuery, Guid rackId);

    Task<Result<Paginateable<IEnumerable<ProductBinCardInformationDto>>>> GetProductBinCardInformation(int page, int pageSize,
        string searchQuery, Guid productId);
}
