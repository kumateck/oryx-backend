using APP.Utils;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using SHARED;
using SHARED.Requests;

namespace APP.IRepository;

public interface IRequisitionRepository
{ 
    Task<Result<Guid>> CreateRequisition(CreateRequisitionRequest request, Guid userId);
    Task<Result<RequisitionDto>> GetRequisition(Guid requisitionId);
    Task<Result<Paginateable<IEnumerable<RequisitionDto>>>> GetRequisitions(int page, int pageSize,
        string searchQuery,  RequestStatus? status);
    Task<Result> ApproveRequisition(ApproveRequisitionRequest request, Guid requisitionId, Guid userId, List<Guid> roleIds);
     Task<Result> ProcessRequisition(CreateRequisitionRequest request, Guid requisitionId, Guid userId);
     Task<Result<Guid>> CreateSourceRequisition(CreateSourceRequisitionRequest request, Guid userId);
    Task<Result<SourceRequisitionDto>> GetSourceRequisition(Guid sourceRequisitionId);
    Task<Result<Paginateable<IEnumerable<SourceRequisitionDto>>>> GetSourceRequisitions(int page,
        int pageSize, string searchQuery);
    Task<Result<Paginateable<IEnumerable<SourceRequisitionItemDto>>>> GetSourceRequisitionItems(int page,
        int pageSize, ProcurementSource source); 
    Task<Result> UpdateSourceRequisition(CreateSourceRequisitionRequest request, Guid sourceRequisitionId); 
    Task<Result> DeleteSourceRequisition(Guid sourceRequisitionId);

    Task<Result<Paginateable<IEnumerable<SupplierQuotationRequest>>>> GetSuppliersWithSourceRequisitionItems(int page,
        int pageSize,  ProcurementSource source, bool sent);

   Task<Result<SupplierQuotationRequest>> GetSuppliersWithSourceRequisitionItems(Guid supplierId);

   Task<Result> SendQuotationToSupplier(Guid supplierId, Guid userId);

   Task<Result<Paginateable<IEnumerable<SupplierQuotationDto>>>> GetSupplierQuotations(int page, int pageSize,
       SupplierType supplierType, bool received);
  Task<Result<SupplierQuotationDto>> GetSupplierQuotation(Guid supplierId);
  Task<Result> ReceiveQuotationFromSupplier(List<SupplierQuotationResponseDto> supplierQuotationResponse,
      Guid supplierId, Guid userId);
  Task<Result<List<SupplierPriceComparison>>> GetPriceComparisonOfMaterial(ProcurementSource source);
  Task<Result> ProcessQuotationAndCreatePurchaseOrder(List<ProcessQuotation> processQuotations,
      Guid userId);
}