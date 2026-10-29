using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.Requisitions;
using SHARED;

namespace DOMAIN.Entities.Products.Production;

/// <summary>Read-only evidence for one production step, including completed work.</summary>
public sealed class ProductionStepRecordsDto
{
    public Guid StepId { get; set; }
    public Guid ProductionScheduleProductId { get; set; }
    public List<ProductionStepAnalysisDto> AnalyticalTests { get; set; } = [];
    public List<BatchManufacturingRecordDto> BatchRecords { get; set; } = [];
    public List<RequisitionDto> Requisitions { get; set; } = [];
    public List<ProductionStepExtraMaterialDto> ExtraMaterialRequests { get; set; } = [];
    public List<FinalPackingDto> FinalPackings { get; set; } = [];
    public List<FinishedGoodsTransferNoteDto> Transfers { get; set; } = [];
    public List<MaterialReturnNoteDto> MaterialReturns { get; set; } = [];
    // Some legacy records have only a batch FK. Never claim they were performed
    // on this step merely because its current instruction has the same action.
    public List<Guid> BatchScopedRecordIds { get; set; } = [];
}

public sealed class ProductionStepAnalysisDto
{
    public AnalyticalTestRequestDto Request { get; set; }
    public Guid? ChemicalArdId { get; set; }
    public Guid? MicrobialArdId { get; set; }
    public bool MicrobialRequired { get; set; }
    public DateTime? TestedAt { get; set; }
    public CollectionItemDto TestedBy { get; set; }
}

public sealed class ProductionStepExtraMaterialDto
{
    public Guid Id { get; set; }
    public CollectionItemDto Material { get; set; }
    public CollectionItemDto Unit { get; set; }
    public decimal Quantity { get; set; }
    public int Status { get; set; }
    public bool Approved { get; set; }
    public DateTime CreatedAt { get; set; }
    public CollectionItemDto CreatedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public CollectionItemDto IssuedBy { get; set; }
}
