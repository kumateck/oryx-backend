using System.Text.Json;
using DOMAIN.Entities.QualityRoutines;
using DOMAIN.Entities.Forms;
using APP.Services.Formulas;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class RoutineQcRepository
{
    public async Task<Result<List<RoutineArdDto>>> ListArds()
    {
        var ards = await context.RoutineArds.Include(item => item.CoaItems)
            .OrderBy(item => item.Type).ThenBy(item => item.AnalysisType).ToListAsync();
        return ards.Select(item => new RoutineArdDto
        {
            Id = item.Id, Type = item.Type, AnalysisType = item.AnalysisType,
            FormId = item.FormId, SpecNumber = item.SpecNumber, IsVerified = item.IsVerified,
            CoaItems = item.CoaItems.Select(field => new RoutineCoaItemRequest
            {
                FormFieldId = field.FormFieldId, IncludeOnCoa = field.IncludeOnCoa,
                DisplayLabel = field.DisplayLabel, GroupName = field.GroupName,
                SpecificationText = field.SpecificationText, Unit = field.Unit,
                Reference = field.Reference, DisplayOrder = field.DisplayOrder
            }).ToList()
        }).ToList();
    }

    public async Task<Result<List<RoutineDefinitionDto>>> ListDefinitions()
    {
        var definitions = await context.RoutineDefinitions.OrderBy(item => item.Type)
            .ThenBy(item => item.Name).ToListAsync();
        return definitions.Select(item => new RoutineDefinitionDto
        {
            Id = item.Id, Name = item.Name, Type = item.Type,
            Cadence = item.Cadence, IsActive = item.IsActive
        }).ToList();
    }

    public async Task<Result<RoutineCertificateDto>> GetCertificate(Guid id)
    {
        var item = await context.RoutineCertificates.FirstOrDefaultAsync(item => item.Id == id);
        if (item is null) return Error.NotFound("RoutineCertificate", "Certificate was not found.");
        return new RoutineCertificateDto
        {
            Id = item.Id, RoutineExecutionId = item.RoutineExecutionId,
            RoutineSampleId = item.RoutineSampleId,
            CertificateCode = item.CertificateCode, Combined = item.Combined,
            RowsJson = item.RowsJson, IssuedAt = item.IssuedAt
        };
    }

    public async Task<Result<List<RoutineExecutionDto>>> ListExecutions()
    {
        var runs = await context.RoutineExecutions
            .Include(item => item.Samples).ThenInclude(item => item.Tracks)
            .ThenInclude(item => item.Response)
            .OrderByDescending(item => item.RoutineDate).ToListAsync();
        return runs.Select(ToDto).ToList();
    }

    public async Task<Result<RoutineTrackContextDto>> GetTrack(Guid id)
    {
        var track = await context.RoutineTracks
            .Include(item => item.Response)
            .Include(item => item.RoutineSample)
            .ThenInclude(item => item.RoutineExecution)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (track is null) return Error.NotFound("RoutineTrack", "Routine analysis track was not found.");
        var sample = track.RoutineSample;
        return new RoutineTrackContextDto
        {
            Id = track.Id, RoutineExecutionId = sample.RoutineExecutionId,
            RoutineCode = sample.RoutineExecution.RoutineCode,
            RoutineSampleId = sample.Id,
            SampleIdentity = sample.RoutineExecution.Type == RoutineType.Water
                ? sample.SamplingPoint : sample.AreaName,
            AnalysisType = track.AnalysisType, FormId = track.FormId,
            RoutineArdId = track.RoutineArdId, ResponseId = track.Response?.Id,
            Approved = track.Response?.Approved == true,
            Rejected = track.Response?.Rejected == true
        };
    }

    public async Task<Result<RoutineExecutionDto>> GetExecution(Guid id)
    {
        var run = await context.RoutineExecutions
            .Include(item => item.Samples).ThenInclude(item => item.Tracks)
            .ThenInclude(item => item.Response)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (run is null) return Error.NotFound("RoutineExecution", "Routine execution was not found.");
        return ToDto(run);
    }

    public async Task<Result> SubmitTrackForApproval(Guid trackId, Guid actorId)
    {
        var track = await context.RoutineTracks
            .Include(item => item.RoutineSample).ThenInclude(item => item.RoutineExecution)
            .FirstOrDefaultAsync(item => item.Id == trackId);
        if (track is null) return Error.NotFound("RoutineTrack", "Routine analysis track was not found.");
        var response = await context.Responses.Include(item => item.FormResponses)
            .FirstOrDefaultAsync(item => item.RoutineTrackId == trackId);
        if (response is null)
            return Error.Validation("RoutineTrack.Worksheet", "Complete the worksheet before submitting approval.");
        if (response.FormId != track.FormId)
            return Error.Conflict("RoutineTrack.Form", "Worksheet form differs from the ARD snapshot.");
        if (!track.WorksheetFinalizedAt.HasValue)
            return Error.Conflict("RoutineTrack.NotFinalized", "Finalize the worksheet before QA approval.");
        if (response.CheckedAt.HasValue)
            return Error.Conflict("RoutineTrack.Submitted", "Worksheet was already checked.");
        var missing = await FormRevisionSelection.MissingRequiredFieldsAsync(context, response);
        if (missing.Count > 0)
            return Error.Validation("RoutineTrack.MissingFields",
                $"Required worksheet fields are missing: {string.Join(", ", missing)}");
        var approvalConfig = await context.Approvals
            .Include(item => item.ApprovalStages)
            .FirstOrDefaultAsync(item => item.ItemType == nameof(Response));
        if (approvalConfig is null || approvalConfig.ApprovalStages.Count == 0)
            return Error.Conflict("RoutineTrack.ApprovalWorkflow",
                "Configure a response approval workflow before submitting routine analysis.");
        var validation = await ResponseApprovalSubmission.ValidateAsync(context, response.Id);
        if (validation.IsFailure) return validation.Errors;
        response.CheckedAt = DateTime.UtcNow;
        response.CheckedById = actorId;
        response.LastUpdatedById = actorId;
        track.RoutineSample.RoutineExecution.Status = RoutineStatus.Submitted;
        context.RoutineAuditEvents.Add(new RoutineAuditEvent
        {
            Id = Guid.NewGuid(), RoutineExecutionId = track.RoutineSample.RoutineExecutionId,
            ActorId = actorId, OccurredAt = DateTime.UtcNow,
            Action = "WorksheetSubmitted",
            Detail = $"{track.AnalysisType}: response {response.Id}",
            CreatedById = actorId
        });
        await approvalRepository.CreateInitialApprovalsAsync(nameof(Response), response.Id);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Guid>> GenerateCertificate(Guid sampleId, Guid actorId)
    {
        var anchor = await context.RoutineSamples
            .FirstOrDefaultAsync(item => item.Id == sampleId);
        if (anchor is null) return Error.NotFound("RoutineSample", "Routine sample was not found.");
        var run = await context.RoutineExecutions
            .Include(item => item.Samples).ThenInclude(item => item.Tracks)
            .ThenInclude(item => item.Response).ThenInclude(item => item.FormResponses)
            .FirstAsync(item => item.Id == anchor.RoutineExecutionId);
        var environmental = run.Type == RoutineType.Environmental;
        if (environmental)
        {
            if (await context.RoutineCertificates.AnyAsync(item =>
                item.RoutineExecutionId == run.Id && item.RoutineSampleId == null))
                return Error.Conflict("RoutineCertificate.Exists",
                    "This environmental monitoring execution already has a certificate.");
        }
        else if (await context.RoutineCertificates.AnyAsync(item =>
            item.RoutineSampleId == sampleId))
            return Error.Conflict("RoutineCertificate.Exists",
                "This water sample already has an immutable certificate.");
        var samples = environmental ? run.Samples.ToList()
            : run.Samples.Where(item => item.Id == sampleId).ToList();
        if (samples.Count == 0)
            return Error.Conflict("RoutineCertificate.Empty", "No monitored observations were recorded.");
        var required = environmental
            ? new[] { AnalysisType.Microbial }
            : new[] { AnalysisType.Chemical, AnalysisType.Microbial };
        if (samples.Any(sample =>
            required.Any(type => sample.Tracks.Count(item => item.AnalysisType == type) != 1) ||
            sample.Tracks.Any(item => item.Response is null || !item.Response.Approved ||
                item.Response.Rejected)))
            return Error.Conflict("RoutineCertificate.Pending",
                "Every required worksheet for the certificate scope needs final approval.");
        var rows = new List<object>();
        foreach (var sample in samples.OrderBy(item => item.AreaName ?? item.SamplingPoint))
        foreach (var track in sample.Tracks.OrderBy(item => item.AnalysisType))
        {
            var reportable = JsonSerializer.Deserialize<List<ReportableItem>>(
                track.CoaItemsSnapshotJson ?? "[]") ?? [];
            foreach (var item in reportable)
            {
                var result = track.Response.FormResponses
                    .SingleOrDefault(value => value.FormFieldId == item.FormFieldId);
                if (result is null || string.IsNullOrWhiteSpace(result.Value) ||
                    result.Complies == false || result.Value.Trim() == "-")
                    return Error.Conflict("RoutineCertificate.MissingResult",
                        $"A reportable item has no approved compliant result: {item.DisplayLabel}.");
                rows.Add(new
                {
                    sampleId = sample.Id,
                    sampleIdentity = environmental ? sample.AreaName : sample.SamplingPoint,
                    track.AnalysisType, trackId = track.Id,
                    responseId = track.Response.Id, track.RoutineArdId,
                    track.Response.FormRevisionId, item.FormFieldId, item.DisplayLabel,
                    item.GroupName, item.SpecificationText, item.Unit, item.Reference,
                    result = result.Value, result.Complies
                });
            }
        }
        var certificate = new RoutineCertificate
        {
            Id = Guid.NewGuid(), RoutineExecutionId = run.Id,
            RoutineSampleId = environmental ? null : sampleId,
            CertificateCode = $"COA/RUT/{DateTime.UtcNow:yyyy}/{Guid.NewGuid():N}",
            Combined = !environmental,
            RowsJson = JsonSerializer.Serialize(rows), IssuedAt = DateTime.UtcNow,
            IssuedById = actorId, CreatedById = actorId
        };
        context.RoutineCertificates.Add(certificate);
        context.RoutineAuditEvents.Add(new RoutineAuditEvent
        {
            Id = Guid.NewGuid(), RoutineExecutionId = run.Id,
            ActorId = actorId, OccurredAt = DateTime.UtcNow,
            Action = "CertificateGenerated", Detail = certificate.CertificateCode,
            CreatedById = actorId
        });
        await context.SaveChangesAsync();
        return certificate.Id;
    }

    private static RoutineExecutionDto ToDto(RoutineExecution run) => new()
    {
        Id = run.Id, RoutineCode = run.RoutineCode, Origin = run.Origin,
        Type = run.Type, Cadence = run.Cadence, RoutineDate = run.RoutineDate,
        PeriodStart = run.PeriodStart, PeriodEnd = run.PeriodEnd,
        EmergencyTrigger = run.EmergencyTrigger, EmergencyReason = run.EmergencyReason,
        RndTrialBatchId = run.RndTrialBatchId, DoneById = run.DoneById,
        DoneAt = run.DoneAt, Status = run.Status,
        Samples = run.Samples.Select(sample => new RoutineSampleDto
        {
            Id = sample.Id, SamplingPoint = sample.SamplingPoint,
            AreaName = sample.AreaName, CollectedAt = sample.CollectedAt,
            Tracks = sample.Tracks.Select(track => new RoutineTrackDto
            {
                Id = track.Id, AnalysisType = track.AnalysisType,
                RoutineArdId = track.RoutineArdId, FormId = track.FormId,
                ResponseId = track.Response?.Id, Approved = track.Response?.Approved == true,
                Rejected = track.Response?.Rejected == true
            }).ToList()
        }).ToList()
    };
}
