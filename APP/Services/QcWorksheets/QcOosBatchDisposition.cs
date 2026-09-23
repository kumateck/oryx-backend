using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets;

/// <summary>
/// The single place in the rebuilt QC module that writes to a pre-existing live entity.
/// <para>
/// This is deliberate and scoped, not a coexistence violation. Batch status is shared
/// system-of-record state that Warehouse, Production and the rest of the ERP read; a QC-only
/// shadow quarantine flag nobody outside QC could see would make a quarantine
/// pharmaceutically meaningless. Everything else this module does stays additive.
/// </para>
/// <para>
/// <b>Exactly two fields are written</b>, both already existing:
/// <c>MaterialBatch.Status</c> (plus <c>MaterialBatch.DateRejected</c>, mirroring what the live
/// <c>OosInvestigation.ReviewByQa</c> already sets) and
/// <c>BatchManufacturingRecord.Status</c>. No schema change is made to either table, and no
/// other Material/Product entity is touched. The mapping is isolated here rather than inlined
/// into the repository so that the one pharmaceutically consequential decision in this
/// milestone is auditable in one file.
/// </para>
///
/// <para><b>Material — exact precedent, no judgement involved.</b> <see cref="BatchStatus"/>
/// carries Quarantine, Available and Rejected, and the live <c>OosInvestigation</c> already
/// writes all three for exactly these transitions.</para>
///
/// <para><b>Product — a real mapping, after a governance ruling.</b>
/// <see cref="BatchManufacturingStatus"/> originally carried no Quarantine and no Available
/// value, and the live <c>OosInvestigation</c> never wrote this field at all — that entity has
/// only a <c>MaterialBatchId</c>, so its quarantine/release behaviour had only ever applied to
/// material batches. An earlier draft of this file approximated the two states with
/// <c>Testing</c> and <c>Approved</c>. That was rejected, correctly: an approximated quarantine
/// is close to no quarantine at all, because Warehouse and Production read this field to decide
/// whether a batch may be used and <c>Testing</c> does not tell them a batch is locked out.
/// <see cref="BatchManufacturingStatus.Quarantine"/> and
/// <see cref="BatchManufacturingStatus.Available"/> were therefore added to the live enum —
/// appended, so no already-persisted numeric value shifts. The Product mapping is now the
/// direct counterpart of the Material one rather than a stand-in.</para>
///
/// <para><b>Release only what this case actually holds.</b> A favourable close moves the batch
/// back only when it is still sitting in the state this case put it in. If something else has
/// moved it since, it is left alone rather than being force-released by a QC module that no
/// longer holds it. A ConfirmedOOS rejection is unconditional, because rejecting is always the
/// safe direction.</para>
/// </summary>
internal static class QcOosBatchDisposition
{
    /// <summary>
    /// The material statuses this module refuses to overwrite with a quarantine. Both are
    /// terminal: a rejected batch is already worse than quarantined, and a consumed one no
    /// longer exists to quarantine.
    /// </summary>
    private static readonly BatchStatus[] TerminalMaterialStatuses =
        [BatchStatus.Rejected, BatchStatus.Consumed];

    /// <summary>The material status a quarantine puts a batch into, and the only one a favourable close will move back out of.</summary>
    internal const BatchStatus MaterialQuarantine = BatchStatus.Quarantine;

    /// <summary>The Product counterpart, and the only status a favourable close will move back out of.</summary>
    internal const BatchManufacturingStatus ProductQuarantine = BatchManufacturingStatus.Quarantine;

    /// <summary>
    /// Quarantines a material batch, returning the status it held beforehand so the case can
    /// record it. Returns null when the batch was left alone.
    /// </summary>
    internal static BatchStatus? Quarantine(MaterialBatch batch)
    {
        if (batch is null) return null;

        var previous = batch.Status;

        // Already terminal: leave it. Quarantining a rejected or consumed batch would be a
        // downgrade of a stronger statement, not an escalation.
        if (TerminalMaterialStatuses.Contains(previous))
            return previous;

        batch.Status = MaterialQuarantine;
        batch.UpdatedAt = DateTime.UtcNow;
        return previous;
    }

    /// <summary>The Product counterpart, holding the record in a real, system-wide quarantine.</summary>
    internal static BatchManufacturingStatus? Quarantine(BatchManufacturingRecord record)
    {
        if (record is null) return null;

        var previous = record.Status;

        if (previous == BatchManufacturingStatus.Rejected)
            return previous;

        record.Status = ProductQuarantine;
        record.UpdatedAt = DateTime.UtcNow;
        return previous;
    }

    /// <summary>
    /// Applies the disposition outcome to a material batch.
    /// <para>
    /// ConfirmedOOS rejects unconditionally and stamps <c>DateRejected</c>, exactly as the live
    /// <c>OosInvestigation.ReviewByQa</c> does. Invalidated and RetestAccepted release, but only
    /// from the quarantine this case applied.
    /// </para>
    /// </summary>
    internal static void Dispose(MaterialBatch batch, OosDispositionOutcome outcome)
    {
        if (batch is null) return;

        if (outcome == OosDispositionOutcome.ConfirmedOOS)
        {
            batch.Status = BatchStatus.Rejected;
            batch.DateRejected = DateTime.UtcNow;
            batch.UpdatedAt = DateTime.UtcNow;
            return;
        }

        // Invalidated / RetestAccepted. Only release what this case is still holding.
        if (batch.Status != MaterialQuarantine) return;

        batch.Status = BatchStatus.Available;
        batch.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// The Product counterpart, now the direct mirror of the material path.
    /// <para>
    /// Release goes to <see cref="BatchManufacturingStatus.Available"/> rather than
    /// <see cref="BatchManufacturingStatus.Approved"/>: both mean the batch is usable, but
    /// keeping them apart preserves <i>why</i> it is usable — a normal QA release
    /// (<c>ResponseFinalApproval</c>) and a favourable OOS closure are different provenance,
    /// and an OOS audit asks exactly that question. Reports meaning "released, not rejected"
    /// must count both; <c>ReportRepository.GetBmrReleaseRate</c> does.
    /// </para>
    /// </summary>
    internal static void Dispose(BatchManufacturingRecord record, OosDispositionOutcome outcome)
    {
        if (record is null) return;

        if (outcome == OosDispositionOutcome.ConfirmedOOS)
        {
            record.Status = BatchManufacturingStatus.Rejected;
            record.UpdatedAt = DateTime.UtcNow;
            return;
        }

        if (record.Status != ProductQuarantine) return;

        record.Status = BatchManufacturingStatus.Available;
        record.UpdatedAt = DateTime.UtcNow;
    }
}
