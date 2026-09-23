using System.Text.Json;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class RoutineQcRepository(
    ApplicationDbContext context,
    IApprovalRepository approvalRepository,
    IConfigurationRepository configurationRepository
) : IRoutineQcRepository
{
    public async Task<Result<Guid>> CreateArd(CreateRoutineArdRequest request, Guid actorId)
    {
        if (!Enum.IsDefined(request.Type) || !Enum.IsDefined(request.AnalysisType))
            return Error.Validation("RoutineArd.Type", "Select a valid routine and analysis type.");
        if (request.Type == RoutineType.Environmental && request.AnalysisType != AnalysisType.Microbial)
            return Error.Validation("RoutineArd.Type", "Environmental monitoring is microbial only.");
        if (!await context.Forms.AnyAsync(item => item.Id == request.FormId))
            return Error.NotFound("RoutineArd.Form", "Worksheet form was not found.");
        if (await context.RoutineArds.AnyAsync(item =>
            item.Type == request.Type && item.AnalysisType == request.AnalysisType))
            return Error.Conflict("RoutineArd.Exists", "An ARD already exists for this routine analysis type.");
        var ids = request.CoaItems.Select(item => item.FormFieldId).ToList();
        if (ids.Count != ids.Distinct().Count())
            return Error.Validation("RoutineArd.CoaItems", "Reportable field selections must be unique.");
        var validFields = await context.FormFields.CountAsync(item =>
            ids.Contains(item.Id) && item.FormSection.FormId == request.FormId);
        if (validFields != ids.Count)
            return Error.Validation("RoutineArd.CoaItems", "Every selected field must belong to the worksheet form.");
        if (!request.CoaItems.Any(item => item.IncludeOnCoa))
            return Error.Validation("RoutineArd.CoaItems", "Select at least one certificate item.");
        if (request.CoaItems.Any(item => item.IncludeOnCoa &&
            (string.IsNullOrWhiteSpace(item.DisplayLabel) || string.IsNullOrWhiteSpace(item.SpecificationText))))
            return Error.Validation("RoutineArd.CoaItems", "Reportable items require a label and specification.");
        var ard = new RoutineArd
        {
            Id = Guid.NewGuid(), Type = request.Type, AnalysisType = request.AnalysisType,
            FormId = request.FormId, SpecNumber = request.SpecNumber?.Trim(),
            Description = request.Description?.Trim(), CreatedById = actorId,
            CoaItems = request.CoaItems.Select(item => new RoutineCoaItem
            {
                Id = Guid.NewGuid(), FormFieldId = item.FormFieldId,
                IncludeOnCoa = item.IncludeOnCoa, DisplayLabel = item.DisplayLabel?.Trim(),
                GroupName = item.GroupName?.Trim(), SpecificationText = item.SpecificationText?.Trim(),
                Unit = item.Unit?.Trim(), Reference = item.Reference?.Trim(),
                DisplayOrder = item.DisplayOrder, CreatedById = actorId
            }).ToList()
        };
        context.RoutineArds.Add(ard);
        await context.SaveChangesAsync();
        return ard.Id;
    }

    public async Task<Result<Guid>> CreateDefinition(CreateRoutineDefinitionRequest request, Guid actorId)
    {
        if (!Enum.IsDefined(request.Type) || !Enum.IsDefined(request.Cadence))
            return Error.Validation("RoutineDefinition", "Select a valid type and monthly or quarterly cadence.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("RoutineDefinition.Name", "Name is required.");
        var definition = new RoutineDefinition
        {
            Id = Guid.NewGuid(), Name = request.Name.Trim(), Type = request.Type,
            Cadence = request.Cadence, IsActive = true, CreatedById = actorId
        };
        context.RoutineDefinitions.Add(definition);
        await context.SaveChangesAsync();
        return definition.Id;
    }

    public async Task<Result<Guid>> CreateExecution(CreateRoutineExecutionRequest request, Guid actorId)
    {
        if (!Enum.IsDefined(request.Type) || !Enum.IsDefined(request.Origin))
            return Error.Validation("RoutineExecution.Type", "Select a valid routine type and origin.");
        if (request.RoutineDate == default)
            return Error.Validation("RoutineExecution.Date", "Routine date is required.");
        if (request.DoneById.HasValue &&
            !await context.Users.AnyAsync(item => item.Id == request.DoneById))
            return Error.NotFound("RoutineExecution.DoneBy",
                "The selected routine performer was not found.");
        RoutineDefinition definition = null;
        if (request.Origin == RoutineOrigin.Scheduled)
        {
            if (!request.RoutineDefinitionId.HasValue || !request.PeriodStart.HasValue ||
                !request.PeriodEnd.HasValue || request.PeriodStart >= request.PeriodEnd)
                return Error.Validation("RoutineExecution.Period", "Scheduled routines require a definition and valid period.");
            definition = await context.RoutineDefinitions.FirstOrDefaultAsync(item =>
                item.Id == request.RoutineDefinitionId && item.IsActive && item.Type == request.Type);
            if (definition is null)
                return Error.Validation("RoutineExecution.Definition", "Active routine definition does not match type.");
            var start = request.PeriodStart.Value.Date;
            var end = request.PeriodEnd.Value.Date;
            var months = definition.Cadence == RoutineCadence.Monthly ? 1 : 3;
            if (start.Day != 1 || start.AddMonths(months) != end)
                return Error.Validation("RoutineExecution.Period", "Period must match the definition's calendar cadence.");
        }
        else if (request.RoutineDefinitionId.HasValue || request.PeriodStart.HasValue ||
            request.PeriodEnd.HasValue || string.IsNullOrWhiteSpace(request.EmergencyTrigger) ||
            string.IsNullOrWhiteSpace(request.EmergencyReason))
            return Error.Validation("RoutineExecution.Emergency", "Emergency routines have no schedule and require trigger and reason.");
        if (request.RndTrialBatchId.HasValue &&
            !await context.RndTrialBatches.AnyAsync(item => item.Id == request.RndTrialBatchId))
            return Error.NotFound("RoutineExecution.RndBatch", "R&D batch was not found.");
        var codeResult = await GenerateRoutineCode();
        if (codeResult.IsFailure)
            return codeResult.Error;
        var run = new RoutineExecution
        {
            Id = Guid.NewGuid(), RoutineCode = codeResult.Value,
            Type = request.Type, Origin = request.Origin, Cadence = definition?.Cadence,
            RoutineDefinitionId = definition?.Id, PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd, RoutineDate = request.RoutineDate,
            EmergencyTrigger = request.EmergencyTrigger?.Trim(),
            EmergencyReason = request.EmergencyReason?.Trim(),
            RndTrialBatchId = request.RndTrialBatchId,
            DoneById = request.DoneById ?? actorId,
            Status = RoutineStatus.Planned, CreatedById = actorId,
            AuditEvents = [new RoutineAuditEvent
            {
                Id = Guid.NewGuid(), ActorId = actorId, OccurredAt = DateTime.UtcNow,
                Action = "Created", Detail = request.Origin == RoutineOrigin.Emergency
                    ? request.EmergencyReason.Trim() : $"Period {request.PeriodStart:yyyy-MM-dd} to {request.PeriodEnd:yyyy-MM-dd}",
                CreatedById = actorId
            }]
        };
        context.RoutineExecutions.Add(run);
        await context.SaveChangesAsync();
        return run.Id;
    }

    public async Task<Result<Guid>> AddSample(Guid executionId, CreateRoutineSampleRequest request, Guid actorId)
    {
        var run = await context.RoutineExecutions.FirstOrDefaultAsync(item => item.Id == executionId);
        if (run is null) return Error.NotFound("RoutineExecution", "Routine execution was not found.");
        if (run.Status is RoutineStatus.Approved or RoutineStatus.Rejected)
            return Error.Conflict("RoutineExecution.Final", "Approved or rejected routine cannot take new samples.");
        if (request.CollectedAt == default)
            return Error.Validation("RoutineSample.Date", "Sample collection date is required.");
        if (run.Type == RoutineType.Water && string.IsNullOrWhiteSpace(request.SamplingPoint))
            return Error.Validation("RoutineSample.Point", "Water sample requires a sampling point.");
        if (run.Type == RoutineType.Environmental && string.IsNullOrWhiteSpace(request.AreaName))
            return Error.Validation("RoutineSample.Area", "Environmental sample requires a monitored room or area.");
        var types = run.Type == RoutineType.Water
            ? new[] { AnalysisType.Chemical, AnalysisType.Microbial }
            : new[] { AnalysisType.Microbial };
        var ards = await context.RoutineArds.Include(item => item.CoaItems)
            .Where(item => item.Type == run.Type && item.IsVerified).ToListAsync();
        if (types.Any(type => ards.All(item => item.AnalysisType != type)))
            return Error.Validation("RoutineSample.Ard", "Every required analysis needs a verified ARD.");
        var sample = new RoutineSample
        {
            Id = Guid.NewGuid(), RoutineExecutionId = run.Id,
            SamplingPoint = request.SamplingPoint?.Trim(), AreaName = request.AreaName?.Trim(),
            CollectedAt = request.CollectedAt, CreatedById = actorId,
            Tracks = types.Select(type =>
            {
                var ard = ards.Single(item => item.AnalysisType == type);
                return new RoutineTrack
                {
                    Id = Guid.NewGuid(), AnalysisType = type, RoutineArdId = ard.Id,
                    FormId = ard.FormId,
                    CoaItemsSnapshotJson = JsonSerializer.Serialize(ard.CoaItems
                        .Where(item => item.IncludeOnCoa).OrderBy(item => item.DisplayOrder)
                        .Select(item => new ReportableItem(item.FormFieldId, item.DisplayLabel,
                            item.GroupName, item.SpecificationText, item.Unit, item.Reference))),
                    CreatedById = actorId
                };
            }).ToList()
        };
        context.RoutineSamples.Add(sample);
        run.Status = RoutineStatus.InProgress;
        run.AuditEvents.Add(new RoutineAuditEvent
        {
            Id = Guid.NewGuid(), ActorId = actorId, OccurredAt = DateTime.UtcNow,
            Action = "SampleAdded", Detail = run.Type == RoutineType.Water
                ? sample.SamplingPoint : sample.AreaName, CreatedById = actorId
        });
        await context.SaveChangesAsync();
        return sample.Id;
    }

    private async Task<Result<string>> GenerateRoutineCode()
    {
        var config = await context.Configurations.FirstOrDefaultAsync(c =>
            c.ModelType == nameof(RoutineExecution));
        if (config is null)
            return Error.Validation("RoutineExecution.Code",
                "No code configuration exists for routine executions.");
        var seriesCount = await configurationRepository.GetCountForCodeConfiguration(
            nameof(RoutineExecution), config.Prefix);
        if (seriesCount.IsFailure)
            return Error.Validation("RoutineExecution.Code",
                "No code configuration exists for routine executions.");
        return CodeGenerator.GenerateCode(config, seriesCount.Value);
    }

    internal record ReportableItem(Guid FormFieldId, string DisplayLabel, string GroupName,
        string SpecificationText, string Unit, string Reference);
}
