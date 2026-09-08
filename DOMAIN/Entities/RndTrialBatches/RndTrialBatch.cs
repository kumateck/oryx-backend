using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.RndTrialBatches;

public class RndTrialBatch : BaseEntity
{
    public Guid RndProjectId { get; set; }
    public RndProject RndProject { get; set; }
    public Guid RndFormulationId { get; set; }
    public RndFormulation RndFormulation { get; set; }
    [StringLength(100)] public string BatchCode { get; set; }
    public RndTrialBatchScaleType ScaleType { get; set; }
    public decimal BatchSize { get; set; }
    public Guid? BatchSizeUoMId { get; set; }
    public UnitOfMeasure BatchSizeUoM { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public Guid? PerformedById { get; set; }
    public User PerformedBy { get; set; }
    public Guid? ProtocolFormId { get; set; }
    public Form ProtocolForm { get; set; }
    public RndTrialBatchStatus Status { get; set; }
    [StringLength(4000)] public string Observations { get; set; }
}

public enum RndTrialBatchScaleType
{
    LabScale = 0,
    PilotScale = 1,
    ExhibitScale = 2,
}

public enum RndTrialBatchStatus
{
    Planned = 0,
    InProgress = 1,
    Completed = 2,
    Aborted = 3,
}

public class RndTrialBatchDto : BaseDto
{
    public Guid RndProjectId { get; set; }
    public Guid RndFormulationId { get; set; }
    public string BatchCode { get; set; }
    public RndTrialBatchScaleType ScaleType { get; set; }
    public decimal BatchSize { get; set; }
    public CollectionItemDto BatchSizeUoM { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public UserDto PerformedBy { get; set; }
    public Guid? ProtocolFormId { get; set; }
    public RndTrialBatchStatus Status { get; set; }
    public string Observations { get; set; }
}

public class CreateRndTrialBatchRequest
{
    public Guid RndFormulationId { get; set; }
    public RndTrialBatchScaleType ScaleType { get; set; }
    public decimal BatchSize { get; set; }
    public Guid? BatchSizeUoMId { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public Guid? PerformedById { get; set; }
    public Guid? ProtocolFormId { get; set; }
    [StringLength(4000)] public string Observations { get; set; }
}

public class UpdateRndTrialBatchStatusRequest
{
    public RndTrialBatchStatus Status { get; set; }
    [StringLength(4000)] public string Observations { get; set; }
}
