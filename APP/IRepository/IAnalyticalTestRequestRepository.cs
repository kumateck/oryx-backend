using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Products.Equipments;
using SHARED;

namespace APP.IRepository;

public interface IAnalyticalTestRequestRepository
{
    Task<Result<Guid>> CreateAnalyticalTestRequest(CreateAnalyticalTestRequest request);

    Task<Result<Paginateable<IEnumerable<AnalyticalTestRequestDto>>>> GetAnalyticalTestRequests(int page, int pageSize, string searchQuery, AnalyticalTestStatus? status);

    Task<Result<AnalyticalTestRequestDto>> GetAnalyticalTestRequest(Guid id);

    Task<Result> UpdateAnalyticalTestRequest(Guid id, CreateAnalyticalTestRequest request);
    Task<Result> UpdateAnalyticalTestRequest(Guid id, UpdateAnalyticalTestRequest request, Guid userId);

    Task<Result> DeleteAnalyticalTestRequest(Guid id, Guid userId);
    Task<Result<AnalyticalTestRequestDto>> GetAnalyticalTestRequestByActivityStep(Guid activityStepId);


    // -----------------------------
    // QC EQUIPMENT CRUD
    // -----------------------------

    Task<Result<Guid>> CreateQcEquipment(CreateQcEquipment request, Guid userId);
    Task<Result<QcEquipmentDto>> GetQcEquipment(Guid id);
    Task<Result<Paginateable<IEnumerable<QcEquipmentDto>>>> GetQcEquipments(int page, int pageSize, string searchQuery);
    Task<Result<List<QcEquipmentDto>>> GetQcEquipments();
    Task<Result> UpdateQcEquipment(CreateQcEquipment request, Guid equipmentId, Guid userId);
    Task<Result> DeleteQcEquipment(Guid id, Guid userId);
}