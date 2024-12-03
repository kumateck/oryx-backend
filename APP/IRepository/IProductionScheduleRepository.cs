using APP.Utils;
using DOMAIN.Entities.ProductionSchedules;
using SHARED;

namespace APP.IRepository;

public interface IProductionScheduleRepository
{
    Task<Result<Guid>> CreateMasterProductionSchedule(CreateMasterProductionScheduleRequest request,
        Guid userId);
    Task<Result<MasterProductionScheduleDto>> GetMasterProductionSchedule(Guid masterScheduleId);
    Task<Result<Paginateable<IEnumerable<MasterProductionScheduleDto>>>> GetMasterProductionSchedules(
        int page, int pageSize, string searchQuery);
    Task<Result> UpdateMasterProductionSchedule(UpdateMasterProductionScheduleRequest request,
        Guid masterScheduleId, Guid userId);
    Task<Result> DeleteMasterProductionSchedule(Guid masterScheduleId, Guid userId);
    Task<Result<Guid>> CreateProductionSchedule(CreateProductionScheduleRequest request, Guid userId);
    Task<Result<ProductionScheduleDto>> GetProductionSchedule(Guid scheduleId);
    Task<Result<Paginateable<IEnumerable<ProductionScheduleDto>>>> GetProductionSchedules(int page,
        int pageSize, string searchQuery);
    Task<Result> UpdateProductionSchedule(UpdateProductionScheduleRequest request, Guid scheduleId,
        Guid userId);
    Task<Result> DeleteProductionSchedule(Guid scheduleId, Guid userId);
    Task<Result<List<ProductionScheduleProcurementDto>>> GetProductionScheduleDetail(
        Guid scheduleId, Guid userId);
}