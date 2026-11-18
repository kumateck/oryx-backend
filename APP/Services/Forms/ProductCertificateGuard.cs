using APP.Services.Authorization;
using APP.Utils;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Forms;

public static class ProductCertificateGuard
{
    public static async Task<Result> Validate(ApplicationDbContext db, Guid bmrId, Guid stepId, Guid actorId)
    {
        if (!(await CurrentPermissionSet.Load(db, actorId)).Contains(PermissionKeys.CanCheckProductTest))
            return Error.Forbidden("ProductCertificate.Permission", "You cannot generate a product certificate.");
        var test = await db.AnalyticalTestRequests.AsNoTracking().Where(a => a.BatchManufacturingRecordId == bmrId && a.ProductionActivityStepId == stepId)
            .OrderByDescending(a => a.CreatedAt).Select(a => new { a.SampledAt, a.AcknowledgedAt, a.TestedAt }).FirstOrDefaultAsync();
        if (test is null) return Error.NotFound("ProductCertificate.Test", "The product test was not found.");
        return Dates(test.SampledAt, test.AcknowledgedAt, test.TestedAt, DateTime.UtcNow);
    }

    public static Result Dates(DateTime? sampled, DateTime? received, DateTime? analysed, DateTime now)
    {
        if (sampled is null || received is null || analysed is null)
            return Error.Conflict("ProductCertificate.MissingDates", "Record the sampling, receipt and analysis dates before generating the certificate.");
        if (sampled.Value.Date > now.Date || received.Value.Date > now.Date || analysed.Value.Date > now.Date)
            return Error.Conflict("ProductCertificate.FutureDate", "Sampling, receipt and analysis dates cannot be in the future.");
        if (sampled.Value.Date > received.Value.Date || received.Value.Date > analysed.Value.Date)
            return Error.Conflict("ProductCertificate.DateOrder", "The sample must be received on or after sampling and on or before analysis. Correct the recorded dates before generating the certificate.");
        return Result.Success();
    }
}
