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
/// <para><b>Product — no 1:1 mapping exists, and the live system offers no precedent.</b>
/// <see cref="BatchManufacturingStatus"/> is a different enum with a different shape:
/// <c>New | Testing | Approved | Rejected | TestTaken | Checked</c>. It has <b>no Quarantine
/// value and no Available value</b>, and the live <c>OosInvestigation</c> never writes it at
/// all — that entity carries only a <c>MaterialBatchId</c>, so its quarantine/release
/// behaviour has only ever applied to material batches. The two Product mappings below are
/// therefore genuine design decisions rather than mirrors of existing behaviour, and are
/// flagged as such for governance:
/// <list type="bullet">
///   <item><description><b>Quarantine =&gt; <see cref="BatchManufacturingStatus.Testing"/></b>.
///   The closest available "held with QC, not releasable" state, and the value
///   <c>ProductAnalyticalRawDataRepository</c> already sets when a record goes out for QC
///   testing. It is lossy: it does not say "quarantined", and a record already in Testing shows
///   no visible change. A dedicated Quarantine enum value would express this properly, but
///   adding one is a schema change to a live table and is out of this milestone's scope.</description></item>
///   <item><description><b>Release =&gt; <see cref="BatchManufacturingStatus.Approved"/></b>.
///   The Available-equivalent, matching the brief's "Invalidated/RetestAccepted =&gt; Available"
///   rule. Note this means closing an OOS case favourably marks a product batch Approved, which
///   is the same terminal state the normal QA release path (<c>ResponseFinalApproval</c>) writes
///   — flagged because an OOS closure is arguably not the same authority as a batch
///   release.</description></item>
/// </list>
/// </para>
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

    /// <summary>See the Product note in the type remarks — this is a decision, not a mirror.</summary>
    internal const BatchManufacturingStatus ProductQuarantine = BatchManufacturingStatus.Testing;

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

    /// <summary>The Product counterpart. See the type remarks for why <c>Testing</c> stands in for a quarantine.</summary>
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

    /// <summary>The Product counterpart. See the type remarks for why <c>Approved</c> stands in for Available.</summary>
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

        record.Status = BatchManufacturingStatus.Approved;
        record.UpdatedAt = DateTime.UtcNow;
    }
}
