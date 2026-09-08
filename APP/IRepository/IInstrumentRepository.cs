using APP.Utils;
using DOMAIN.Entities.Instruments;
using SHARED;

namespace APP.IRepository;

public interface IInstrumentRepository
{
    Task<Result<Paginateable<IEnumerable<InstrumentDto>>>> GetInstruments(int page, int pageSize, string searchQuery);
    Task<Result<InstrumentDto>> GetInstrument(Guid id);
    Task<Result> UpdateCalibration(Guid instrumentId, UpdateInstrumentCalibrationRequest request, Guid userId);
    Task<Result<List<InstrumentDto>>> GetInstrumentsWithCalibrationDue(int withinDays, DateTime? asOf = null);
}
