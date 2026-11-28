using System.Text.Json;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace APP.Repository;

/// <summary>
/// The facts printed above and below an issued certificate's results, as on the paper certificate: what was tested, its batch,
/// dates and AR number, the product's composition, pack style and shelf life, and who analysed, checked and approved it.
/// Captured when the certificate is issued, so the certificate stays a fixed copy even if those records change later.
/// </summary>
internal static class CommercialCertificateHeader
{
    public sealed record Person(string Name, string? Designation);

    public sealed record Header(
        string Subject, string? Name, string? Code, string? BatchNumber, DateTime? ManufacturingDate, DateTime? ExpiryDate,
        string? ArNumber, DateTime? SampledAt, DateTime? AnalysedAt, string? Composition, string? PackStyle, string? ShelfLife,
        List<Person> AnalysedBy, List<Person> CheckedBy, List<Person> ApprovedBy);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<string> BuildAsync(ApplicationDbContext db, Guid? sampleId, Guid? atrId, IReadOnlyCollection<Response> responses)
    {
        Header header;
        if (sampleId.HasValue)
        {
            var sample = await db.MaterialSamplings.AsNoTracking().Where(item => item.Id == sampleId)
                .Select(item => new
                {
                    item.ArNumber, item.SampleDate, item.MaterialBatch.BatchNumber, item.MaterialBatch.ManufacturingDate, item.MaterialBatch.ExpiryDate,
                    item.MaterialBatch.Material.Name, item.MaterialBatch.Material.Code,
                }).SingleAsync();
            header = new("Material", sample.Name, sample.Code, sample.BatchNumber, sample.ManufacturingDate, sample.ExpiryDate,
                sample.ArNumber, sample.SampleDate, AnalysedAt(responses), null, null, null, [], [], []);
        }
        else
        {
            var test = await db.AnalyticalTestRequests.AsNoTracking().Where(item => item.Id == atrId)
                .Select(item => new
                {
                    item.ArNumber, item.SampledAt, item.TestedAt, item.ManufacturingDate, item.ExpiryDate, item.BatchManufacturingRecord.BatchNumber,
                    item.ProductionScheduleProduct.Product.Name, item.ProductionScheduleProduct.Product.Code,
                    item.ProductionScheduleProduct.Product.LabelClaim, item.ProductionScheduleProduct.Product.PackageStyle,
                    item.ProductionScheduleProduct.Product.ShelfLife,
                }).SingleAsync();
            header = new("Product", test.Name, test.Code, test.BatchNumber, test.ManufacturingDate, test.ExpiryDate,
                test.ArNumber, test.SampledAt, test.TestedAt ?? AnalysedAt(responses), Blank(test.LabelClaim), Blank(test.PackageStyle), Blank(test.ShelfLife), [], [], []);
        }

        // Analysts are the authors of the reported answers; the checker generated the certificate; the approver signed the final
        // required stage of the current approval round, as the certificate preview shows them.
        var analysts = responses.SelectMany(response => response.FormResponses ?? []).Where(answer => answer.CreatedById.HasValue)
            .Select(answer => answer.CreatedById!.Value).Distinct().ToList();
        var checkers = responses.Where(response => response.CheckedById.HasValue).Select(response => response.CheckedById!.Value).Distinct().ToList();
        var approvers = new List<Guid>();
        foreach (var response in responses)
        {
            var approvals = await db.ResponseApprovals.AsNoTracking().Where(item => item.ResponseId == response.Id).ToListAsync();
            var final = ResponseApprovalRoundManager.Current(approvals)
                .Where(item => item.Required && item.Status == ApprovalStatus.Approved)
                .OrderByDescending(item => item.Order).ThenByDescending(item => item.ApprovalTime).FirstOrDefault();
            if (final?.ApprovedById is { } approver && !approvers.Contains(approver)) approvers.Add(approver);
        }
        var people = await People(db, [.. analysts, .. checkers, .. approvers]);
        header = header with
        {
            AnalysedBy = analysts.Select(id => people.GetValueOrDefault(id)).OfType<Person>().ToList(),
            CheckedBy = checkers.Select(id => people.GetValueOrDefault(id)).OfType<Person>().ToList(),
            ApprovedBy = approvers.Select(id => people.GetValueOrDefault(id)).OfType<Person>().ToList(),
        };
        return JsonSerializer.Serialize(header, Json);
    }

    /// <summary>The day results were analysed: when the first worksheet was checked.</summary>
    private static DateTime? AnalysedAt(IReadOnlyCollection<Response> responses) =>
        responses.Where(response => response.CheckedAt.HasValue).Select(response => response.CheckedAt).Min();

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Each user's name and, when they are an employee with one, their designation.</summary>
    private static async Task<Dictionary<Guid, Person>> People(ApplicationDbContext db, IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0) return [];
        var users = await db.Users.IgnoreQueryFilters().AsNoTracking().Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, user.FirstName, user.LastName }).ToListAsync();
        var designations = await db.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(employee => employee.UserId.HasValue && ids.Contains(employee.UserId.Value) && employee.Designation != null)
            .Select(employee => new { UserId = employee.UserId!.Value, employee.Designation.Name }).ToListAsync();
        return users.ToDictionary(user => user.Id, user => new Person(
            $"{user.FirstName} {user.LastName}".Trim() is { Length: > 0 } name ? name : "Unnamed user",
            designations.FirstOrDefault(item => item.UserId == user.Id)?.Name));
    }
}
