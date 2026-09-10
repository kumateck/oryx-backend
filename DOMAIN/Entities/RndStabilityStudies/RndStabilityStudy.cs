using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.RndTrialBatches;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.RndStabilityStudies;

public class RndStabilityStudy : BaseEntity
{
    public Guid RndProjectId { get; set; }
    public RndProject RndProject { get; set; }
    public Guid RndTrialBatchId { get; set; }
    public RndTrialBatch RndTrialBatch { get; set; }
    public Guid RndStabilityChamberId { get; set; }
    public RndStabilityChamber RndStabilityChamber { get; set; }
    public Guid? ProtocolFormId { get; set; }
    public Form ProtocolForm { get; set; }
    public DateTime StartDate { get; set; }
    public RndStabilityStudyStatus Status { get; set; }
    public List<RndStabilityPullPoint> PullPoints { get; set; } = [];
}

public enum RndStabilityStudyStatus
{
    Active = 0,
    Completed = 1,
    Terminated = 2,
}

public class RndStabilityPullPoint : BaseEntity
{
    public Guid RndStabilityStudyId { get; set; }
    public RndStabilityStudy RndStabilityStudy { get; set; }
    public int TimePointMonths { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PulledAt { get; set; }
    public Guid? PulledById { get; set; }
    public User PulledBy { get; set; }
    public RndStabilityPullPointStatus Status { get; set; }
    [StringLength(4000)] public string ResultsSummary { get; set; }
}

public enum RndStabilityPullPointStatus
{
    Scheduled = 0,
    Pulled = 1,
    Reported = 2,
}

public class RndStabilityStudyDto : BaseDto
{
    public Guid RndProjectId { get; set; }
    public Guid RndTrialBatchId { get; set; }
    public CollectionItemDto RndStabilityChamber { get; set; }
    public Guid? ProtocolFormId { get; set; }
    public DateTime StartDate { get; set; }
    public RndStabilityStudyStatus Status { get; set; }
    public List<RndStabilityPullPointDto> PullPoints { get; set; } = [];
}

public class RndStabilityPullPointDto : BaseDto
{
    public int TimePointMonths { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PulledAt { get; set; }
    public UserDto PulledBy { get; set; }
    public RndStabilityPullPointStatus Status { get; set; }
    public string ResultsSummary { get; set; }
}

public class CreateRndStabilityStudyRequest
{
    public Guid RndTrialBatchId { get; set; }
    public Guid RndStabilityChamberId { get; set; }
    public Guid? ProtocolFormId { get; set; }
    public DateTime StartDate { get; set; }
    public List<int> PullPointTimePointsMonths { get; set; } = [];
}

public class RecordPullPointResultRequest
{
    [StringLength(4000)] public string ResultsSummary { get; set; }
}

public class UpdateRndStabilityStudyStatusRequest
{
    public RndStabilityStudyStatus Status { get; set; }
}
