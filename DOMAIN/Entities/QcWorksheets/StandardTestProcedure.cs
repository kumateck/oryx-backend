using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.QcWorksheets;

/// <summary>
/// A controlled Standard Test Procedure document in the rebuilt QC module.
/// <para>
/// Entirely separate from, and coexisting with, the live
/// <c>MaterialStandardTestProcedure</c>/<c>ProductStandardTestProcedure</c> tables, which
/// this does not read, write, or modify.
/// </para>
/// <para>
/// Approvals are not held as a child collection: they live in the shared
/// <see cref="QcApproval"/> table, looked up by
/// (EntityType = "StandardTestProcedure", EntityId = this Id).
/// </para>
/// </summary>
public class StandardTestProcedure : BaseEntity, IRequireApproval
{
    /// <summary>From <see cref="IRequireApproval"/>; set true once every required approval stage is approved.</summary>
    public bool Approved { get; set; }

    [StringLength(100)] public string Code { get; set; }

    [StringLength(500)] public string Name { get; set; }

    [StringLength(200)] public string Area { get; set; }

    /// <summary>Displayed as "Revision No." in the STP editor UI; named for schema consistency with <see cref="WorksheetTemplate.Version"/>.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Self-referencing: the version this one was created from.</summary>
    public Guid? SupersedesId { get; set; }

    public StandardTestProcedure Supersedes { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public DateTime? ReviewDate { get; set; }

    public DateTime? IssueDate { get; set; }

    public string Purpose { get; set; }

    public string Scope { get; set; }

    public string Responsibility { get; set; }

    public string Accountability { get; set; }

    public QcDocumentStatus Status { get; set; } = QcDocumentStatus.Draft;

    public List<StpStep> Steps { get; set; } = [];
}

public class StpStep : BaseEntity
{
    public Guid StandardTestProcedureId { get; set; }

    public StandardTestProcedure StandardTestProcedure { get; set; }

    public int Order { get; set; }

    [StringLength(200)] public string Title { get; set; }

    public string Instruction { get; set; }

    /// <summary>
    /// The structured cross-reference to another STP. Nullable because not every step
    /// references another document.
    /// </summary>
    public Guid? ReferencedStpId { get; set; }

    public StandardTestProcedure ReferencedStp { get; set; }
}
