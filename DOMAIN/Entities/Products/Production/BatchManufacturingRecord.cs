using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Products.Production;

public class CreateBatchManufacturingRecord
{
    public Guid ProductionScheduleProductId { get; set; }
    public Guid ProductionActivityStepId { get; set; }

    [Required]
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
}

public class UpdateBatchManufacturingRecord
{
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
    public Guid? ProductPackingId { get; set; }
}

public class BatchManufacturingRecord : BaseEntity
{
    public Guid ProductionScheduleProductId { get; set; }
    public ProductionScheduleProduct ProductionScheduleProduct { get; set; }
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }

    [StringLength(1000)]
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
    public decimal SampledQuantity { get; set; }
    public BatchManufacturingStatus Status { get; set; }
    public Guid? IssuedById { get; set; }
    public User IssuedBy { get; set; }
    public DateTime? IssuedDate { get; set; }
}

public enum BatchManufacturingStatus
{
    New = 0,
    Testing = 1,
    Approved = 2,
    Rejected = 3,
    TestTaken = 4,
    Checked = 5,

    /// <summary>
    /// Held by a formal QC OOS investigation and not available for use anywhere in the ERP.
    /// <para>
    /// Added alongside <see cref="Available"/> so a product batch can carry a real, unambiguous
    /// quarantine rather than having <see cref="Testing"/> stand in for one. An approximated
    /// status is close to no status at all here: Warehouse and Production read this field to
    /// decide whether a batch may be used, and "Testing" does not tell them a batch is locked
    /// out. This mirrors <c>BatchStatus.Quarantine</c>, which the material side has always had.
    /// </para>
    /// <para>
    /// Written only by the QC OOS workflow (<c>QcOosBatchDisposition</c>), on starting an
    /// investigation, and cleared by that same workflow's QA disposition.
    /// </para>
    /// </summary>
    Quarantine = 6,

    /// <summary>
    /// Released back into use after a QC OOS case closed favourably — the original result was
    /// invalidated, or a retest was accepted.
    /// <para>
    /// Deliberately distinct from <see cref="Approved"/>, which is the normal QA release path
    /// (<c>ResponseFinalApproval</c>). Both mean the batch is usable; keeping them separate
    /// preserves <i>why</i> it is usable, which is exactly the kind of provenance an OOS audit
    /// asks for. Reports that mean "released, not rejected" should count both — see
    /// <c>ReportRepository.GetBmrReleaseRate</c>.
    /// </para>
    /// </summary>
    Available = 7,
}

public class BatchManufacturingRecordDto : BaseDto
{
    public ProductionScheduleProductDto ProductionScheduleProduct { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BatchQuantity { get; set; }
    public BatchManufacturingStatus Status { get; set; }
    public decimal SampledQuantity { get; set; }
    public DateTime? IssuedDate { get; set; }
}
