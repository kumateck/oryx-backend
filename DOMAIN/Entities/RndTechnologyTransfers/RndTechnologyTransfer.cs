using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.RndTechnologyTransfers;

public class RndTechnologyTransfer : BaseEntity
{
    public Guid RndProjectId { get; set; }
    public RndProject RndProject { get; set; }
    public Guid RndFormulationId { get; set; }
    public RndFormulation RndFormulation { get; set; }

    // Structured around WHO TRS 1044 Annex 4's due-diligence/gap-analysis chapters
    // (organization & management, documentation, equipment/instrument qualification,
    // life-cycle approach) via the existing dynamic Form module - not a fixed schema.
    public Guid? GapAnalysisFormId { get; set; }
    public Form GapAnalysisForm { get; set; }

    public RndTechnologyTransferStatus Status { get; set; }
    public Guid? BillOfMaterialId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedById { get; set; }
    public User CompletedBy { get; set; }
}

public enum RndTechnologyTransferStatus
{
    DueDiligence = 0,
    GapAnalysis = 1,
    ProtocolApproved = 2,
    Completed = 3,
}

public class RndTechnologyTransferDto : BaseDto
{
    public Guid RndProjectId { get; set; }
    public Guid RndFormulationId { get; set; }
    public Guid? GapAnalysisFormId { get; set; }
    public RndTechnologyTransferStatus Status { get; set; }
    public Guid? BillOfMaterialId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public UserDto CompletedBy { get; set; }
}

public class CreateRndTechnologyTransferRequest
{
    public Guid RndFormulationId { get; set; }
    public Guid? GapAnalysisFormId { get; set; }
}

public class UpdateRndTechnologyTransferStatusRequest
{
    public RndTechnologyTransferStatus Status { get; set; }
}
