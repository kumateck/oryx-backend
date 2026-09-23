using APP.Utils;
using DOMAIN.Entities.QualityRoutines;
using SHARED;

namespace APP.IRepository;

public interface IWaterQualityRepository
{
    Task<Result<Guid>> ActivatePeriod(ActivateWaterQualityPeriodRequest request,
        Guid actorId);
    Task<Result<Guid>> RecordUse(RecordWaterUseRequest request, Guid actorId);
    Task<Result<int>> HoldPeriod(Guid id, HoldWaterQualityPeriodRequest request,
        Guid actorId);
    Task<Result<List<WaterQualityPeriodDto>>> ListPeriods();
    Task<Result<Paginateable<IEnumerable<EligibleWaterCertificateDto>>>>
        ListEligibleCertificates(int page, int pageSize, string searchQuery);
}
