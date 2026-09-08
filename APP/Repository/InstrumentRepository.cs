using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Instruments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class InstrumentRepository(ApplicationDbContext context, IMapper mapper) : IInstrumentRepository
{
    public async Task<Result<Paginateable<IEnumerable<InstrumentDto>>>> GetInstruments(
        int page,
        int pageSize,
        string searchQuery
    )
    {
        var query = context.Instruments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, i => i.Code, i => i.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<InstrumentDto>
        );
    }

    public async Task<Result<InstrumentDto>> GetInstrument(Guid id)
    {
        var instrument = await context.Instruments.FirstOrDefaultAsync(i => i.Id == id);
        return instrument is null
            ? Error.NotFound("Instrument.NotFound", "Instrument not found.")
            : mapper.Map<InstrumentDto>(instrument);
    }

    public async Task<Result> UpdateCalibration(
        Guid instrumentId,
        UpdateInstrumentCalibrationRequest request,
        Guid userId
    )
    {
        var instrument = await context.Instruments.FirstOrDefaultAsync(i => i.Id == instrumentId);
        if (instrument is null)
            return Error.NotFound("Instrument.NotFound", "Instrument not found.");

        if (
            request.CalibrationCertificateAttachmentId.HasValue
            && !await context.Attachments.AnyAsync(a =>
                a.Id == request.CalibrationCertificateAttachmentId.Value
            )
        )
            return Error.NotFound(
                "Attachment.NotFound",
                "Calibration certificate attachment not found."
            );

        instrument.CalibrationDueDate = request.CalibrationDueDate;
        instrument.LastCalibratedAt = request.LastCalibratedAt;
        instrument.CalibrationCertificateAttachmentId = request.CalibrationCertificateAttachmentId;
        instrument.QualificationStatus = request.QualificationStatus;
        instrument.LastUpdatedById = userId;

        context.Instruments.Update(instrument);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<InstrumentDto>>> GetInstrumentsWithCalibrationDue(
        int withinDays,
        DateTime? asOf = null
    )
    {
        if (withinDays < 0)
            return Error.Validation("Instrument.Days", "Within-days must be zero or greater.");

        var start = (asOf ?? DateTime.UtcNow).Date;
        var end = start.AddDays(withinDays);

        var instruments = await context
            .Instruments.Where(i =>
                i.CalibrationDueDate.HasValue
                && i.CalibrationDueDate.Value.Date >= start
                && i.CalibrationDueDate.Value.Date <= end
            )
            .OrderBy(i => i.CalibrationDueDate)
            .ToListAsync();

        return mapper.Map<List<InstrumentDto>>(instruments);
    }
}
