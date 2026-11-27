using APP.Services.Authorization;
using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.StpRevisions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.StpRevisions;

public sealed partial class StpRevisionService
{
    /// <summary>Reads an uploaded Word STP into a proposal. Nothing is saved.</summary>
    public async Task<Result<StpImportProposalDto>> Import(StpRevisionKind kind, IFormFile file, Guid actorId)
    {
        var refused = await Refuse(StpRevisionTask.Import, kind, actorId);
        if (refused is not null) return refused;
        if (DocxUploadGuard.Validate(file) is { } invalid) return Error.Validation("StpRevision.File", invalid);
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;
        var proposal = importer.Propose(stream, Path.GetFileName(file.FileName), kind);
        await Reconcile(proposal);
        return proposal;
    }

    /// <summary>
    /// Reads the current version of an STP's controlled Word document (the approved version, else the latest draft)
    /// into a proposal. The document itself is left as it is.
    /// </summary>
    public async Task<Result<StpImportProposalDto>> ImportDocument(ImportStpDocumentRequest request, Guid actorId)
    {
        var refused = await Refuse(StpRevisionTask.Import, request.Kind, actorId);
        if (refused is not null) return refused;
        string ownerType;
        string stpNumber;
        if (request.Kind == StpRevisionKind.Product)
        {
            ownerType = nameof(ProductStandardTestProcedure);
            stpNumber = await context.ProductStandardTestProcedures.AsNoTracking().Where(item => item.Id == request.StpId)
                .Select(item => item.StpNumber).FirstOrDefaultAsync();
        }
        else
        {
            ownerType = nameof(MaterialStandardTestProcedure);
            var materialKind = request.Kind == StpRevisionKind.RawMaterial ? MaterialKind.Raw : MaterialKind.Package;
            stpNumber = await context.MaterialStandardTestProcedures.AsNoTracking()
                .Where(item => item.Id == request.StpId && item.Material.Kind == materialKind)
                .Select(item => item.StpNumber).FirstOrDefaultAsync();
        }
        if (stpNumber is null) return Error.NotFound("StpRevision.Stp", "STP not found.");
        var document = await context.StpDocuments.AsNoTracking()
            .Where(item => item.OwnerType == ownerType && item.OwnerId == request.StpId)
            .Select(item => new { item.Id, VersionId = item.EffectiveVersionId ?? item.CurrentDraftVersionId })
            .FirstOrDefaultAsync();
        if (document?.VersionId is not { } versionId)
            return Error.NotFound("StpRevision.Document", "This STP has no Word document to import.");
        var version = await context.StpDocumentVersions.AsNoTracking().Where(item => item.Id == versionId)
            .Select(item => new { item.VersionNumber, item.FileName }).FirstOrDefaultAsync();
        var file = await documents.GetVersionFile(document.Id, versionId);
        if (file.IsFailure) return Error.NotFound("StpRevision.Document", "The document's file could not be read.");
        await using var stream = new MemoryStream();
        await using (var source = file.Value.Stream)
        {
            source.Position = 0;
            await source.CopyToAsync(stream);
        }
        stream.Position = 0;
        var proposal = importer.Propose(stream, version?.FileName ?? file.Value.Name, request.Kind);
        proposal.Source = $"STP document version {version?.VersionNumber}";
        if (proposal.Succeeded && !Same(proposal.StpNumber?.Value, stpNumber))
        {
            proposal.StpNumber ??= new StpImportField();
            proposal.StpNumber.Flag = $"The document says {proposal.StpNumber.Value ?? "nothing"}; the STP record is {stpNumber}.";
            proposal.StpNumber.Value = stpNumber;
            proposal.StpNumber.Confidence = StpImportConfidence.Medium;
        }
        await Reconcile(proposal);
        return proposal;
    }

    /// <summary>
    /// Ties the proposal to what is stored: the STP number must be one of that kind, and a test whose title matches a
    /// test of the approved revision takes that test's key, so the key stays stable when the Word file is re-imported.
    /// </summary>
    private async Task Reconcile(StpImportProposalDto proposal)
    {
        if (!proposal.Succeeded) return;
        var numbers = await StpNumbers(proposal.Kind);
        var number = proposal.StpNumber?.Value;
        if (number is not null)
        {
            var known = numbers.FirstOrDefault(item => Same(item, number));
            if (known is null)
            {
                proposal.StpNumber.Confidence = StpImportConfidence.Low;
                proposal.StpNumber.Flag = $"{number} is not an STP number of this kind. Create the STP first, or correct the number.";
            }
            else proposal.StpNumber.Value = known;
            var current = await context.StpRevisions.AsNoTracking()
                .Where(item => item.Kind == proposal.Kind && item.StpNumber == (known ?? number) && item.Status == StpRevisionStatus.Approved)
                .SelectMany(item => item.Tests.Select(test => new { test.TestKey, test.Title })).ToListAsync();
            foreach (var test in proposal.Tests)
            {
                var match = current.FirstOrDefault(item => string.Equals(item.Title?.Trim(), test.Title?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match is not null) test.TestKey = match.TestKey;
            }
            // Keys taken from the approved revision may now collide with suggested ones.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var test in proposal.Tests.Where(test => !seen.Add(test.TestKey))) test.TestKey = null;
            StpDocxContentReader.AssignKeys(proposal.Tests);
        }
        proposal.FlaggedCount = new[] { proposal.StpNumber, proposal.Title, proposal.RevisionNumber, proposal.SupersedesRevision,
                proposal.Area, proposal.Reference, proposal.EffectiveDate, proposal.ReviewDate, proposal.IssueDate }
            .Count(field => field is { Confidence: not StpImportConfidence.High })
            + proposal.Sections.Count(item => item.Confidence != StpImportConfidence.High)
            + proposal.Tests.Count(item => item.Confidence != StpImportConfidence.High)
            + proposal.Tests.Sum(test => test.Steps.Count(item => item.Confidence != StpImportConfidence.High));
    }
}
