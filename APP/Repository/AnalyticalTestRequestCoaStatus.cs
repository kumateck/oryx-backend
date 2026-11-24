using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

internal static class AnalyticalTestRequestCoaStatus
{
    internal static string Status(IReadOnlyCollection<Response> responses)
    {
        if (responses.Count == 0) return "Not generated";
        if (responses.Any(response => response.Rejected)) return "Revision required";
        if (responses.All(response => response.Approved)) return "Approved";
        if (responses.All(response => response.CheckedAt.HasValue || response.Approved)) return "Pending approval";
        return responses.Any(response => response.CheckedAt.HasValue || response.Approved) ? "Partially generated" : "Not generated";
    }
    internal static async Task Apply(ApplicationDbContext db, IReadOnlyCollection<AnalyticalTestRequest> tests, IReadOnlyCollection<AnalyticalTestRequestDto> destinations)
    {
        var batchIds = tests.Select(test => test.BatchManufacturingRecordId).Distinct().ToList();
        var responses = await db.Responses.AsNoTracking().Where(response => response.BatchManufacturingRecordId.HasValue && batchIds.Contains(response.BatchManufacturingRecordId.Value)
            && response.RoutineTrackId == null).ToListAsync();
        var ardIds = tests.SelectMany(test => new[] { test.ChemicalArdId, test.MicrobialArdId }).Where(id => id.HasValue).ToList();
        var forms = await db.ProductAnalyticalRawData.AsNoTracking().Where(ard => ardIds.Contains(ard.Id)).Select(ard => new { ard.Id, ard.FormId }).ToDictionaryAsync(ard => ard.Id, ard => ard.FormId);
        foreach (var test in tests)
        {
            var current = responses.Where(response => response.BatchManufacturingRecordId == test.BatchManufacturingRecordId && response.ProductionActivityStepId == test.ProductionActivityStepId)
                .GroupBy(response => response.FormId).Select(group => group.OrderByDescending(response => response.ReworkSequence).ThenByDescending(response => response.CreatedAt).ThenByDescending(response => response.Id).First()).ToList();
            var required = new[] { test.ChemicalArdId, test.MicrobialRequired ? test.MicrobialArdId : null }.Where(id => id.HasValue && forms.ContainsKey(id.Value))
                .Select(id => forms[id.Value]).Distinct().ToList();
            if (required.Count > 0)
            {
                current = required.Select(form => current.SingleOrDefault(response => response.FormId == form) ?? new Response { FormId = form }).ToList();
            }
            // A required track without a resolved template cannot be treated as generated.
            if (test.MicrobialRequired && (!test.MicrobialArdId.HasValue || !forms.ContainsKey(test.MicrobialArdId.Value)))
                current.Add(new Response());
            if (test.ChemicalArdId.HasValue && !forms.ContainsKey(test.ChemicalArdId.Value))
                current.Add(new Response());
            var dto = destinations.Single(item => item.Id == test.Id);
            dto.CoaStatus = Status(current);
        }
    }
}
